using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Grpc.Data.Models.Sessions;
using GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Features.Sessions;

/// <summary>
/// Soft-deletes drafts nobody has touched for ninety days, and removes sessions that have expired.
/// </summary>
/// <remarks>
/// <b>This is a privacy obligation, not housekeeping.</b> A draft carries a claimant's name and contact
/// details from the moment section 1 is typed, and an abandoned one is a form that will never be
/// submitted - keeping it for the seven years the ACNC asks of <i>submitted</i> records would be
/// retaining personal data for a purpose that has ended. Ninety days from the last edit, not from
/// creation, so a form somebody is slowly working through is never taken from under them.
/// <para>
/// <b>Submitted claims are never touched, at any age.</b> The query filters on
/// <c>Status == Draft</c> and that condition is the whole safety of this service: a bug that widened it
/// would soft-delete financial records under a retention obligation. It is the reason this deletes by
/// loading and flagging rather than with a bulk <c>ExecuteUpdate</c> - the slower version is the one
/// whose predicate is visible next to the thing it protects.
/// </para>
/// <para>
/// <b>The attachment objects are deliberately left in the store.</b> Soft-deleting the rows is
/// reversible; deleting bytes is not, and reclaiming them is a decision about destroying uploaded files
/// that nobody has taken yet. What that costs is logged on every pass - see the "reclaimable" figure -
/// so the size of the question is visible before anyone answers it.
/// </para>
/// <para>
/// Runs in the gRPC service rather than in a worker of its own. With more than one replica every
/// replica runs it, which is harmless: flagging an already-flagged row writes the same value, and
/// nothing here depends on running exactly once.
/// </para>
/// </remarks>
public class DraftPurgeService(
    IServiceProvider services,
    ILogger<DraftPurgeService> logger
) : BackgroundService
{
    /// <summary>
    /// Daily. The threshold is ninety days, so the cost of a pass running late is measured in hours
    /// against a window measured in months - there is nothing to gain from checking more often.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>
    /// Long enough that the database and the object store are up. A failed first pass is harmless -
    /// the next one is a day away and does the same work - but a stack of startup errors in the log on
    /// every deploy trains people to ignore the log.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using PeriodicTimer timer = new(Interval);

            do
            {
                await PurgeAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown. Not a fault, and not worth a log line.
        }
    }

    private async Task PurgeAsync(CancellationToken token)
    {
        try
        {
            using IServiceScope scope = services.CreateScope();

            AccountingDbContext db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset cutoff = now - AnonymousSessionOptions.AbandonedDraftLifetime;

            List<DbExpenseSubmission> abandoned = await db.ExpenseSubmissions
                .Include(x => x.Details).ThenInclude(x => x.Items)
                .Include(x => x.Attachments)
                .Include(x => x.Attendees)
                .Include(x => x.Trips)
                .Include(x => x.MissingReceipt)
                .AsSplitQuery()
                // BOTH conditions matter. Status is what keeps submitted evidence out of this, and
                // UpdatedAt is what makes the window run from the last edit rather than from creation.
                .Where(x => x.Status == SubmissionStatus.Draft && x.UpdatedAt < cutoff)
                .ToListAsync(token);

            long reclaimable = 0;

            foreach (DbExpenseSubmission submission in abandoned)
            {
                reclaimable += submission.Attachments.Sum(x => x.ByteSize);

                ExpenseSubmissionService.SoftDelete(submission);
            }

            // A session outlives the drafts it owned, so this runs second and independently: an expired
            // session whose drafts were purged months ago is simply a dead row. Nothing references it -
            // OwnerSessionId is a plain column, not a foreign key - so removing it orphans nothing, and
            // a session holds no financial record for the retention rule to protect.
            int expiredSessions = await db.AnonymousSessions
                .Where(x => x.ExpiresAt < now)
                .ExecuteDeleteAsync(token);

            if (abandoned.Count > 0)
                await db.SaveChangesAsync(token);

            if (abandoned.Count > 0 || expiredSessions > 0)
            {
                logger.LogInformation(
                    "Purged {Drafts} abandoned draft(s) last edited before {Cutoff:u} and {Sessions} "
                    + "expired session(s). {Reclaimable} bytes of attachment objects are now referenced "
                    + "only by soft-deleted rows and are NOT deleted from the store",
                    abandoned.Count, cutoff, expiredSessions, reclaimable);
            }
        }
        catch (Exception ex)
        {
            // Swallowed on purpose. This is a background tidy-up, and an exception escaping
            // ExecuteAsync stops the service for the life of the process - so a transient database
            // blip would silently end every future pass.
            logger.LogError(ex, "The abandoned-draft purge failed. The next pass will retry");
        }
    }
}

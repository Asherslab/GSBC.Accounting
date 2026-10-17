using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Grpc.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>
/// Sends what <c>OutgoingEmails</c> says is owed, and keeps trying until it goes.
/// </summary>
/// <remarks>
/// <b>Safe with any number of replicas.</b> A row is only worked on after a conditional update has taken
/// its lease, so two replicas polling the same backlog cannot both send one email - which matters here
/// more than usual, because "the accounts team got the claim twice" is how a claim gets paid twice. The
/// one duplicate this cannot prevent is Graph accepting a message and the reply being lost on the way
/// back; the row stays unsent and the retry sends it again. A second copy is the better failure.
/// <para>
/// <b>A failure never stops the loop.</b> Each row's failure is recorded on the row, pushed back by an
/// exponential delay, and logged at Error with Graph's own message. After <see cref="MaxAttempts"/> - a
/// little over a week of trying - it stops and logs at Critical: a claim nobody was emailed about is a
/// claim somebody has to be told about by hand.
/// </para>
/// </remarks>
public class OutgoingEmailService(
    IServiceProvider services,
    MailConfig config,
    MailSignal signal,
    ILogger<OutgoingEmailService> logger
) : BackgroundService
{
    /// <summary>The poll, for work the wake-up missed - a retry coming due, or another replica's submit.</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    /// <summary>Longer than any one send - a 24 MB PDF rendered and uploaded - takes.</summary>
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(6);

    /// <summary>1, 2, 4 ... 256 minutes, then six-hourly: about eight days in all.</summary>
    public const int MaxAttempts = 40;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsSendingEnabled())
            return;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await SendDueAsync(stoppingToken);
                await signal.WaitAsync(PollInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown. Anything mid-send keeps its lease, which lapses, and is retried.
        }
    }

    /// <summary>
    /// Says once, at startup, why nothing will be sent - rather than leaving an empty inbox to explain
    /// itself.
    /// </summary>
    private bool IsSendingEnabled()
    {
        switch (config.Transport)
        {
            case MailTransportKind.None:
                logger.LogWarning(
                    "Mail:Transport is None. Submitted claims are queued in OutgoingEmails and NOT sent");
                return false;

            case MailTransportKind.Graph when !config.IsGraphConfigured:
                logger.LogError(
                    "Mail:Transport is Graph but Mail:TenantId, Mail:ClientId, Mail:ClientSecret or Mail:Sender "
                    + "is missing - is mail-secret created? Submitted claims are queued and NOT sent until it "
                    + "is, and the pod restarted");
                return false;

            case MailTransportKind.Directory when string.IsNullOrWhiteSpace(config.DirectoryPath):
                logger.LogError("Mail:Transport is Directory but Mail:DirectoryPath is empty. Nothing is sent");
                return false;

            default:
                logger.LogInformation("Sending email via {Transport} as {Sender}", config.Transport, config.Sender);
                return true;
        }
    }

    private async Task SendDueAsync(CancellationToken token)
    {
        try
        {
            List<Guid> due;

            using (IServiceScope scope = services.CreateScope())
            {
                AccountingDbContext db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
                DateTimeOffset now = DateTimeOffset.UtcNow;

                due = await db.OutgoingEmails
                    .Where(x => x.SentAt == null && x.Attempts < MaxAttempts && x.NextAttemptAt <= now
                                && (x.LeaseUntil == null || x.LeaseUntil < now))
                    .OrderBy(x => x.NextAttemptAt)
                    .Select(x => x.Id)
                    .Take(20)
                    .ToListAsync(token);
            }

            // One scope per email, so one claim's tracked entities and its PDF bytes are released before
            // the next is loaded.
            foreach (Guid id in due)
            {
                using IServiceScope scope = services.CreateScope();
                await SendOneAsync(scope.ServiceProvider, id, token);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A database blip. Swallowed for the same reason as DraftPurgeService: an exception escaping
            // ExecuteAsync stops this service for the life of the process.
            logger.LogError(ex, "The outgoing email pass failed. The next pass will retry");
        }
    }

    private async Task SendOneAsync(IServiceProvider provider, Guid id, CancellationToken token)
    {
        AccountingDbContext db = provider.GetRequiredService<AccountingDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // THE LEASE. Only the replica whose update matched the row may send it; everyone else sees 0
        // and moves on. The predicate repeats the "still due" conditions so a row another replica has
        // just sent cannot be leased again.
        int taken = await db.OutgoingEmails
            .Where(x => x.Id == id && x.SentAt == null && (x.LeaseUntil == null || x.LeaseUntil < now))
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.LeaseUntil, now + Lease), token);

        if (taken == 0)
            return;

        DbOutgoingEmail email = await db.OutgoingEmails.SingleAsync(x => x.Id == id, token);

        try
        {
            DbExpenseSubmission submission = await db.ExpenseSubmissions
                .Include(x => x.Details).ThenInclude(x => x.Items)
                .Include(x => x.Attachments)
                .Include(x => x.Attendees)
                .Include(x => x.MissingReceipt)
                .AsSplitQuery()
                .SingleAsync(x => x.Id == email.SubmissionId, token);

            ClaimEmails emails = provider.GetRequiredService<ClaimEmails>();

            OutgoingMessage message = email.Kind switch
            {
                OutgoingEmailKind.ClaimToAccounts => await emails.ClaimToAccountsAsync(submission, token),
                OutgoingEmailKind.ReceiptToClaimant => emails.ReceiptToClaimant(submission),
                _ => throw new InvalidOperationException($"No message is defined for {email.Kind}.")
            };

            await provider.GetRequiredService<IMailTransport>().SendAsync(message, token);

            email.SentAt = DateTimeOffset.UtcNow;
            email.Attempts++;
            email.LeaseUntil = null;
            email.LastError = null;

            logger.LogInformation("Sent {Kind} for claim {Reference} to {To}",
                email.Kind, submission.Reference, message.To);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            email.Attempts++;
            email.LeaseUntil = null;
            email.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            email.NextAttemptAt = DateTimeOffset.UtcNow + Backoff(email.Attempts);

            if (email.Attempts >= MaxAttempts)
            {
                logger.LogCritical(ex,
                    "GAVE UP sending {Kind} for submission {SubmissionId} after {Attempts} attempts. Nobody "
                    + "has been emailed about this; tell the accounts team by hand, then reset Attempts on "
                    + "OutgoingEmails row {EmailId} to retry",
                    email.Kind, email.SubmissionId, email.Attempts, email.Id);
            }
            else
            {
                logger.LogError(ex,
                    "Could not send {Kind} for submission {SubmissionId} (attempt {Attempts}). Retrying at {Next:u}",
                    email.Kind, email.SubmissionId, email.Attempts, email.NextAttemptAt);
            }
        }

        // Not the stopping token: the email has gone, and shutting down between the send and this write
        // is exactly when the record of having sent it must not be dropped.
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private static TimeSpan Backoff(int attempts)
    {
        double minutes = Math.Pow(2, Math.Min(attempts - 1, 20));

        return TimeSpan.FromMinutes(Math.Min(minutes, MaxBackoff.TotalMinutes));
    }
}

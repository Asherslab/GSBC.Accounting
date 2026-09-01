using GSBC.Accounting.Grpc.Features.Sessions;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Features.Expenses;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;

public partial class ExpenseSubmissionService
{
    /// <summary>
    /// The calling session's unsubmitted drafts, newest edit first.
    /// </summary>
    /// <remarks>
    /// <b>A browser with no session is told it has no drafts, and is not given one.</b> That is the
    /// half of "mint on first write" that is easy to get wrong: this method is called on every visit to
    /// the drafts page, so minting here would hand a session and a row to every visitor and every
    /// crawler - exactly what creating drafts lazily was meant to avoid.
    /// <para>
    /// Projected to <see cref="DraftSummary"/> in the database rather than fetched and mapped. A
    /// claimant's drafts carry their name, contact details and every line of what they bought; pulling
    /// all of that back to count two numbers would move a great deal of personal data for no reader.
    /// </para>
    /// <para>
    /// The counts respect the soft-delete filter because they are subqueries over the filtered sets -
    /// <c>Details</c> and <c>Attachments</c> both carry a global <c>!Deleted</c> filter, so a receipt
    /// the claimant removed is not counted. That is the behaviour anyone reading "3 receipts" expects,
    /// and it matters more than it did: <c>Update</c> soft-deletes and re-adds every detail on every
    /// autosave, so an unfiltered count would climb by the size of section 3 every two seconds.
    /// </para>
    /// </remarks>
    public async Task<ListDraftsResponse> ListDrafts(
        ListDraftsRequest request,
        CallContext context = default
    )
    {
        CancellationToken token = context.CancellationToken;

        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return ListDraftsResponse.Empty();

        var rows = await db.ExpenseSubmissions
            .Where(x => x.OwnerSessionId == sessionId && x.Status == SubmissionStatus.Draft)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                x.Kind,
                x.SubmitterName,
                x.PurposeActivity,
                x.GrossTotal,
                DetailCount = x.Details.Count,
                AttachmentCount = x.Attachments.Count,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(token);

        // ExpiresAt is added here rather than in the projection. `UpdatedAt + a TimeSpan` inside the
        // query would have to survive translation to a Postgres interval, and the value is arithmetic
        // on a column that has already been fetched - there is nothing to gain from making the database
        // do it and a translation failure to lose.
        List<DraftSummary> drafts = rows
            .Select(x => new DraftSummary
            {
                Id = x.Id,
                Kind = x.Kind,
                SubmitterName = x.SubmitterName,
                PurposeActivity = x.PurposeActivity,
                GrossTotal = x.GrossTotal,
                DetailCount = x.DetailCount,
                AttachmentCount = x.AttachmentCount,
                CreatedAt = x.CreatedAt.UtcDateTime,
                UpdatedAt = x.UpdatedAt.UtcDateTime,
                ExpiresAt = (x.UpdatedAt + AnonymousSessionOptions.AbandonedDraftLifetime).UtcDateTime
            })
            .ToList();

        return new ListDraftsResponse { Success = true, Drafts = drafts };
    }
}

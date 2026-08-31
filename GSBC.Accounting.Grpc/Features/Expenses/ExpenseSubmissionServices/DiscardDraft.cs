using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Base;
using Microsoft.EntityFrameworkCore;
using static GSBC.Accounting.Grpc.Features.Expenses.ErrorConstants;

namespace GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;

public partial class ExpenseSubmissionService
{
    /// <summary>
    /// Soft-deletes one of the calling session's drafts so it leaves their list.
    /// </summary>
    /// <remarks>
    /// <b>The children are flagged too, and the attachment objects are left alone.</b> Flagging the
    /// header only would leave lines and attachment rows that every query still returns, which is how a
    /// discarded draft's receipts end up counted against a submission that no longer exists. The bytes
    /// in the object store stay where they are - nothing in this app deletes them, and this is the same
    /// soft delete the purge does, not a stronger one.
    /// <para>
    /// The button says "Discard", and what a claimant is promised is that the draft leaves their list
    /// and stops being resumable. That is exactly what this does. It does not promise the bytes are
    /// destroyed, and nothing in the UI says it does.
    /// </para>
    /// </remarks>
    public async Task<BasicResponse> DiscardDraft(
        DiscardDraftRequest request,
        CallContext context = default
    )
    {
        CancellationToken token = context.CancellationToken;

        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return BasicResponse.WithError(SubmissionNotFound);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Lines)
            .Include(x => x.Attachments)
            .Include(x => x.Attendees)
            .Include(x => x.Trips)
            .Include(x => x.MissingReceipt)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x => x.Id == request.SubmissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return BasicResponse.WithError(SubmissionNotFound);

        // A submitted claim is evidence, and a claimant cannot withdraw one by pressing a button.
        // Reversing a submission needs a person, and there is no screen for that in this scope.
        if (submission.Status != SubmissionStatus.Draft)
            return BasicResponse.WithError(AlreadySubmitted);

        SoftDelete(submission);

        await db.SaveChangesAsync(token);

        return new BasicResponse { Success = true };
    }

    /// <summary>
    /// Flags the aggregate and everything under it. Shared with the abandoned-draft purge so a draft
    /// thrown away by hand and one that timed out leave the database in the same state.
    /// </summary>
    internal static void SoftDelete(DbExpenseSubmission submission)
    {
        submission.Deleted = true;

        foreach (DbExpenseLine line in submission.Lines)
            line.Deleted = true;

        foreach (DbExpenseAttachment attachment in submission.Attachments)
            attachment.Deleted = true;

        foreach (DbExpenseAttendee attendee in submission.Attendees)
            attendee.Deleted = true;

        foreach (DbExpenseTrip trip in submission.Trips)
            trip.Deleted = true;

        if (submission.MissingReceipt is { } missing)
            missing.Deleted = true;
    }
}

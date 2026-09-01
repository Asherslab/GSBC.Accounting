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
    /// One of the calling session's own drafts, whole, so a form page can be filled back in from it.
    /// </summary>
    /// <remarks>
    /// <b>Drafts only.</b> A submitted claim is deliberately not readable here even by the session that
    /// filed it: it is evidence, the form cannot edit it, and the only thing anyone would do with it on
    /// this page is see an editable copy of something that is no longer editable. The PDF is what a
    /// submitted claim is read as.
    /// <para>
    /// <c>AsSplitQuery</c> for the same reason <c>PdfEndpoints</c> uses it - five collection includes on
    /// one aggregate is a cartesian product, and a submission with eight lines and four attachments
    /// otherwise comes back as thirty-two rows of duplicated header.
    /// </para>
    /// </remarks>
    public async Task<BasicReadResponse<ExpenseSubmission>> Read(
        ReadExpenseSubmissionRequest request,
        CallContext context = default
    )
    {
        CancellationToken token = context.CancellationToken;

        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return BasicReadResponse<ExpenseSubmission>.WithError(SubmissionNotFound);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Details.OrderBy(detail => detail.Ordinal))
            .ThenInclude(x => x.Items.OrderBy(item => item.Ordinal))
            .Include(x => x.Attachments)
            .Include(x => x.Attendees.OrderBy(attendee => attendee.Ordinal))
            .Include(x => x.Trips.OrderBy(trip => trip.Ordinal))
            .Include(x => x.MissingReceipt)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x => x.Id == request.SubmissionId
                     && x.OwnerSessionId == sessionId
                     && x.Status == SubmissionStatus.Draft,
                token);

        if (submission is null)
            return BasicReadResponse<ExpenseSubmission>.WithError(SubmissionNotFound);

        return new BasicReadResponse<ExpenseSubmission>
        {
            Success = true,
            Entity = converter.Convert(submission)
        };
    }
}

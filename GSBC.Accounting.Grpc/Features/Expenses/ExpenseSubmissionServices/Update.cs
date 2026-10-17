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
    /// Rewrites a draft with the form as it now stands.
    /// </summary>
    /// <remarks>
    /// The children are <b>replaced</b>, not merged. A receipt the claimant deleted has to disappear,
    /// and matching rows up by position across an edit that inserted one in the middle is a way to
    /// silently move somebody's money between purchases.
    /// <para>
    /// The rows are soft-deleted rather than removed, like everything else here: seven-year retention
    /// applies to a draft too, and a superseded detail is part of how a submission came to say what it
    /// says.
    /// </para>
    /// <para>
    /// <b>Replacing the details is why <see cref="ExpenseDetail.Key"/> exists.</b> Every autosave gives
    /// each detail a new row id, so the uploaded files point at the claimant's own stable key instead -
    /// which the client sends back unchanged and <c>WriteDetails</c> writes through.
    /// </para>
    /// </remarks>
    public async Task<BasicResponse> Update(UpdateExpenseSubmissionRequest request, CallContext context = default)
    {
        CancellationToken token = context.CancellationToken;

        List<string> errors = ValidateForCreate(request.Form);

        if (errors.Count > 0)
            return BasicResponse.WithErrors(errors);

        // No session, no draft to rewrite. Answered with the same "could not be found" as a genuinely
        // missing row, because the two must be indistinguishable from outside.
        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return BasicResponse.WithError(SubmissionNotFound);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Details).ThenInclude(x => x.Items)
            .Include(x => x.Attachments)
            .Include(x => x.Attendees)
            .Include(x => x.MissingReceipt)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x => x.Id == request.SubmissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return BasicResponse.WithError(SubmissionNotFound);

        // A submitted claim is evidence. It does not change afterwards.
        if (submission.Status != SubmissionStatus.Draft)
            return BasicResponse.WithError(AlreadySubmitted);

        CreateExpenseSubmissionRequest form = request.Form;

        // THE KIND CAN CHANGE, while the submission is still a draft. It used to come from the URL and
        // was fixed at creation; it is now the form's first question, and somebody who mis-answers it
        // has to be able to correct it without retyping the claim.
        //
        // What must not survive the change is the other form's content. ClearFieldsForOtherKind below
        // drops its section 1 fields, and the attendee table falls out of the soft-delete-then-re-add
        // further down, which re-adds it only on the kind that has one.
        submission.Kind = form.Kind;

        (decimal gross, decimal gst, decimal lessPersonal) = ExpenseTotals.SumDetails(form.Details);

        submission.SubmitterName = form.SubmitterName;
        submission.SubmitterEmail = form.SubmitterEmail;
        submission.FormDate = ToOffset(form.FormDate);

        submission.CardLastFourDigits = form.CardLastFourDigits;

        submission.ContactPhone = form.ContactPhone;
        submission.MinistryDepartment = form.MinistryDepartment;
        submission.PaymentMethod = form.PaymentMethod;
        submission.PaymentMethodOther = form.PaymentMethodOther;
        submission.BankDetailsOnFile = form.BankDetailsOnFile;

        // A name with no Yes in front of it is an approval nobody said happened.
        submission.PriorApprovalGiven = form.PriorApprovalGiven;
        submission.PriorApprovalBy = form.PriorApprovalGiven == true ? form.PriorApprovalBy : null;

        submission.GrossTotal = gross;
        submission.GstTotal = gst;
        submission.LessPersonalAmount = lessPersonal;
        submission.NetTotal = ExpenseTotals.Net(gross, lessPersonal);

        submission.ComplianceQ1 = form.ComplianceQ1;
        submission.ComplianceQ2 = form.ComplianceQ2;
        submission.ComplianceQ3 = form.ComplianceQ3;
        submission.ComplianceQ4 = form.ComplianceQ4;
        submission.ComplianceQ5 = form.ComplianceQ5;
        submission.ComplianceQ6 = form.ComplianceQ6;
        submission.ComplianceDetails = form.ComplianceDetails;

        submission.Declaration1 = form.Declaration1;
        submission.Declaration2 = form.Declaration2;
        submission.Declaration3 = form.Declaration3;
        submission.Declaration4 = form.Declaration4;
        submission.Declaration5 = form.Declaration5;

        submission.SignatureName = form.SignatureName;

        // Everything above wrote what the client sent, both kinds' columns alike. This is the one place
        // that decides what a submission of THIS kind is allowed to hold, and it runs last - on every
        // write, not only on a kind change - so a client cannot leave the other form's answers behind by
        // sending them anyway.
        ClearFieldsForOtherKind(submission);

        // What the abandoned-draft purge counts from. A claimant who edits a form every few weeks
        // keeps it indefinitely; one who never comes back loses it ninety days after their last edit,
        // not ninety days after they started.
        submission.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (DbExpenseDetail detail in submission.Details)
        {
            detail.Deleted = true;

            foreach (DbExpenseDetailItem item in detail.Items)
                item.Deleted = true;
        }

        foreach (DbExpenseAttendee attendee in submission.Attendees)
            attendee.Deleted = true;

        WriteDetails(submission, form.Details);

        // A file whose detail the claimant has just deleted is now pointing at nothing. It stays on the
        // submission - nothing here throws evidence away - but the link is cleared, so the PDF lists it
        // under "not filed against a purchase" rather than against a receipt that is no longer on the
        // form. Detaching the file itself is a separate, deliberate act with its own endpoint.
        HashSet<Guid> liveKeys = form.Details.Select(x => x.Key).ToHashSet();

        foreach (DbExpenseAttachment attachment in submission.Attachments)
        {
            if (attachment.DetailKey is { } key && !liveKeys.Contains(key))
                attachment.DetailKey = null;
        }

        if (submission.Kind == SubmissionKind.DebitCardPurchase)
            WriteAttendees(submission, form.Attendees);

        // Section 5's trigger, evaluated against the STORED attachments and the form's details: a
        // purchase with files against it, none of which came from the place it was bought. A detail with
        // no files at all is not this - it is a form somebody is still filling in - so it does not open
        // section 5 and would not be submittable anyway.
        bool hasUnreceiptedPurchase = form.Details.Any(detail =>
        {
            List<DbExpenseAttachment> files = submission.Attachments
                .Where(x => x.DetailKey == detail.Key && !x.Deleted)
                .ToList();

            return files.Count > 0 && files.All(x => x.Kind != AttachmentKind.SupplierReceipt);
        });

        if (hasUnreceiptedPurchase && form.MissingReceipt is { } missing)
        {
            // 0..1, so it is updated in place rather than replaced - a second row would violate the
            // one-per-submission relationship.
            submission.MissingReceipt ??= new DbMissingReceiptDeclaration
            {
                Id = Guid.Empty,
                SubmissionId = submission.Id
            };

            submission.MissingReceipt.Supplier = missing.Supplier;
            submission.MissingReceipt.Date = ToOffset(missing.Date);
            submission.MissingReceipt.Amount = missing.Amount is { } m ? ExpenseTotals.Money(m) : null;
            submission.MissingReceipt.Reason = missing.Reason;
            submission.MissingReceipt.Declared = missing.Declared;
            submission.MissingReceipt.Deleted = false;
        }
        else if (submission.MissingReceipt is not null)
        {
            // Every purchase now has a receipt from where it was bought, so the declaration no longer
            // belongs to this submission.
            submission.MissingReceipt.Deleted = true;
        }

        await db.SaveChangesAsync(token);

        return new BasicResponse { Success = true };
    }

    /// <summary>
    /// Nulls the fields belonging to the kind this submission is not. Run on every write, by both
    /// <c>Create</c> and <c>Update</c>.
    /// </summary>
    /// <remarks>
    /// <b>Only the kind-specific columns.</b> Everything else on the form means the same thing on both
    /// documents - the claimant, the authorisation, the receipts, the missing-receipt declaration - and a
    /// claimant who corrects one question should not lose the afternoon's typing.
    /// <para>
    /// Compliance question 1 is one of them: only the debit card form still asks it, so a reimbursement
    /// holding an answer to it would be holding an answer to a question nobody was shown.
    /// </para>
    /// <para>
    /// The compliance answers and the declarations are <b>not</b> cleared here, because by the time this
    /// runs the client has already sent them cleared: four of the six questions and four of the five
    /// declarations are different text on the two forms, so a tick carried across would record somebody
    /// as having agreed to wording they were never shown. That clearing belongs on the page, where the
    /// claimant is told it is about to happen. This method exists for the fields the page has no reason
    /// to blank, and as the backstop for a client that does not.
    /// </para>
    /// <para>
    /// Section 3 is not touched here either, and that is the point of it: a receipt is a receipt on both
    /// forms. The attendee table is not touched either - every row was already soft-deleted by
    /// <c>Update</c>, and it is re-added only on the debit card form.
    /// </para>
    /// </remarks>
    private static void ClearFieldsForOtherKind(DbExpenseSubmission submission)
    {
        if (submission.Kind == SubmissionKind.DebitCardPurchase)
        {
            submission.ContactPhone = null;
            submission.MinistryDepartment = null;
            submission.PaymentMethod = null;
            submission.PaymentMethodOther = null;
            submission.BankDetailsOnFile = null;
        }
        else
        {
            submission.CardLastFourDigits = null;
            submission.ComplianceQ1 = null;
        }
    }
}

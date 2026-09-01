using System.Globalization;
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
    /// Recomputes the totals from what is stored, checks the form is complete, and marks it submitted.
    /// </summary>
    /// <remarks>
    /// <b>This is where a draft stops being allowed to be half-finished.</b> <c>Create</c> only checks
    /// what must hold for the row to be coherent; every completeness rule lives here, because somebody
    /// filling in a long form needs to be able to save it and come back.
    /// <para>
    /// Everything is checked against the <b>stored</b> submission rather than against a re-sent form.
    /// A request carrying the whole form again would let a client submit something different from what
    /// it uploaded receipts against.
    /// </para>
    /// <para>
    /// "Refused" here always means the submission is incomplete or internally inconsistent. It never
    /// means the app has formed a view on whether an expense is legitimate - that is a reviewer's job,
    /// and nothing in this scope second-guesses it.
    /// </para>
    /// </remarks>
    public async Task<BasicResponse> Submit(SubmitExpenseSubmissionRequest request, CallContext context = default)
    {
        CancellationToken token = context.CancellationToken;

        // Only the session that filled the form in may submit it. Without this, holding an id was
        // enough to turn somebody else's half-finished draft into a claim standing in their name.
        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return BasicResponse.WithError(SubmissionNotFound);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Lines)
            .Include(x => x.Attachments)
            .Include(x => x.MissingReceipt)
            .FirstOrDefaultAsync(
                x => x.Id == request.SubmissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return BasicResponse.WithError(SubmissionNotFound);

        // Idempotent in the sense that matters: a double-click cannot submit twice, and the second
        // attempt says so rather than silently succeeding.
        if (submission.Status != SubmissionStatus.Draft)
            return BasicResponse.WithError(AlreadySubmitted);

        // The totals are recomputed here, not trusted from the row - the row was written by Create from
        // a client's request, and this is the last point before the claim becomes evidence.
        List<ExpenseLine> lines = submission.Lines
            .OrderBy(x => x.Ordinal)
            .Select(x => new ExpenseLine
            {
                SubmissionId = x.SubmissionId,
                Ordinal = x.Ordinal,
                Evidence = x.Evidence,
                GrossAmount = x.GrossAmount,
                GstAmount = x.GstAmount,
                ChurchUsePercent = x.ChurchUsePercent
            })
            .ToList();

        (decimal gross, decimal gst) = ExpenseTotals.SumLines(lines);
        decimal net = ExpenseTotals.Net(gross, submission.LessPersonalAmount);

        List<string> errors = ValidateForSubmit(submission, gross);

        if (errors.Count > 0)
            return BasicResponse.WithErrors(errors);

        submission.GrossTotal = gross;
        submission.GstTotal = gst;
        submission.NetTotal = net;
        submission.Status = SubmissionStatus.Submitted;
        submission.SubmittedAt = DateTimeOffset.UtcNow;
        submission.SignedAt = DateTimeOffset.UtcNow;

        // Marking the properties this method owns rather than calling db.Update, which writes every
        // column and would silently revert anything another writer committed since the read above.
        db.Entry(submission).Property(x => x.GrossTotal).IsModified = true;
        db.Entry(submission).Property(x => x.GstTotal).IsModified = true;
        db.Entry(submission).Property(x => x.NetTotal).IsModified = true;
        db.Entry(submission).Property(x => x.Status).IsModified = true;
        db.Entry(submission).Property(x => x.SubmittedAt).IsModified = true;
        db.Entry(submission).Property(x => x.SignedAt).IsModified = true;

        await db.SaveChangesAsync(token);

        return new BasicResponse { Success = true };
    }

    private static List<string> ValidateForSubmit(DbExpenseSubmission submission, decimal gross)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(submission.SubmitterName))
            errors.Add(NeedsASubmitterName);

        if (string.IsNullOrWhiteSpace(submission.PurposeNarrative))
            errors.Add(NeedsAPurposeNarrative);

        // Section 3 has to say what was bought. A draft is allowed to have no lines at all - somebody
        // fills section 1 in first - so this is asked once, here, and never while they are typing.
        if (submission.Lines.Count == 0)
            errors.Add(SubmissionNeedsALine);

        // Section 3's first column is a different field on each form, so which one is required depends
        // on the kind. This is the smallest example of the rule that runs through the whole app: the two
        // forms share a structure, not their contents.
        switch (submission.Kind)
        {
            case SubmissionKind.DebitCardPurchase
                when submission.Lines.Any(x => string.IsNullOrWhiteSpace(x.ItemDescription)):
                errors.Add(DebitCardLineNeedsAnItem);
                break;

            case SubmissionKind.ExpenseReimbursement when submission.Lines.Any(x => x.LineDate is null):
                errors.Add(ReimbursementLineNeedsADate);
                break;
        }

        bool hasMissingLine = submission.Lines.Any(x => x.Evidence == EvidenceStatus.Missing);
        bool hasReceipt = submission.Attachments.Any(x =>
            x.Kind is AttachmentKind.ItemisedReceipt or AttachmentKind.TaxInvoice);

        // At least one itemised receipt is mandatory; it is the point of the form. The one way out is
        // the one the paper form itself provides - mark the line Missing and complete section 5.
        if (!hasReceipt && !hasMissingLine)
            errors.Add(NeedsEvidence);

        if (hasMissingLine && submission.MissingReceipt is not { Declared: true })
            errors.Add(MissingEvidenceNeedsADeclaration);

        // null is unanswered, and unanswered is not No.
        bool[] answered =
        [
            submission.ComplianceQ1 is not null, submission.ComplianceQ2 is not null,
            submission.ComplianceQ3 is not null, submission.ComplianceQ4 is not null,
            submission.ComplianceQ5 is not null, submission.ComplianceQ6 is not null
        ];

        if (answered.Any(x => !x))
            errors.Add(ComplianceQuestionsUnanswered);

        bool[] declarations =
        [
            submission.Declaration1 == true, submission.Declaration2 == true,
            submission.Declaration3 == true, submission.Declaration4 == true,
            submission.Declaration5 == true
        ];

        if (declarations.Any(x => !x))
            errors.Add(DeclarationsNotAgreed);

        if (string.IsNullOrWhiteSpace(submission.SignatureName))
            errors.Add(NeedsASignature);

        if (submission.Kind == SubmissionKind.DebitCardPurchase)
        {
            // Draft accepts a half-typed "12"; a submitted claim has to carry all four, or the finance
            // reviewer cannot match it against a bank line.
            if (string.IsNullOrWhiteSpace(submission.CardLastFourDigits))
                errors.Add(DebitCardNeedsCardLastFour);
            else if (submission.CardLastFourDigits.Length != 4
                     || !submission.CardLastFourDigits.All(char.IsAsciiDigit))
            {
                errors.Add(CardLastFourDigitsMustBeFourDigits);
            }

            if (submission.AmountCharged is null)
            {
                errors.Add(DebitCardNeedsAmountCharged);
            }
            else
            {
                decimal charged = ExpenseTotals.Money(submission.AmountCharged.Value);

                // THE RECONCILIATION. Both figures are named, because "the totals do not match" sends
                // somebody hunting through a table for a number the server already knows.
                //
                // Only the debit card form has this: it is the one whose total is stated twice, once by
                // the bank and once by the claimant's own itemisation. The reimbursement form has
                // nothing external to reconcile against.
                if (charged != gross)
                {
                    errors.Add(
                        $"The itemised lines total {Money(gross)} but section 1 says the card was "
                        + $"charged {Money(charged)}. Add the missing lines, or correct the amount charged.");
                }
            }
        }

        return errors.Distinct().ToList();
    }

    /// <summary>
    /// en-AU explicitly, not the server's culture. This string is read by a claimant, and a container
    /// that happens to run under en-US or the invariant culture would show them a figure that is right
    /// but formatted like somebody else's money.
    /// </summary>
    private static string Money(decimal value) =>
        value.ToString("C2", CultureInfo.GetCultureInfo("en-AU"));
}

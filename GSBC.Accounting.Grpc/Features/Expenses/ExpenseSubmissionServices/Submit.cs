using System.Globalization;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Features.Expenses;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    public async Task<SubmitExpenseSubmissionResponse> Submit(
        SubmitExpenseSubmissionRequest request,
        CallContext context = default
    )
    {
        CancellationToken token = context.CancellationToken;

        // Only the session that filled the form in may submit it. Without this, holding an id was
        // enough to turn somebody else's half-finished draft into a claim standing in their name.
        if (await sessions.CurrentAsync(token) is not { } sessionId)
            return SubmitExpenseSubmissionResponse.WithError(SubmissionNotFound);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Details).ThenInclude(x => x.Items)
            .Include(x => x.Attachments)
            .Include(x => x.MissingReceipt)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x => x.Id == request.SubmissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return SubmitExpenseSubmissionResponse.WithError(SubmissionNotFound);

        // Idempotent in the sense that matters: a double-click cannot submit twice, and the second
        // attempt says so rather than silently succeeding.
        if (submission.Status != SubmissionStatus.Draft)
            return SubmitExpenseSubmissionResponse.WithError(AlreadySubmitted);

        // The totals are recomputed here, not trusted from the row - the row was written by Create from
        // a client's request, and this is the last point before the claim becomes evidence.
        List<ExpenseDetail> details = submission.Details
            .OrderBy(x => x.Ordinal)
            // EVERY FIELD ValidateForSubmit READS HAS TO BE HERE. This projection is the stored row as
            // the validator sees it, so a field left out of it reads as absent no matter what the
            // database holds - which lands on the claimant as "every purchase needs the place it was
            // bought" printed over a form that plainly says Woolworths. Observed on 2026-09-01, when
            // Supplier, PurchaseDate and Purpose were all missing from here.
            .Select(x => new ExpenseDetail
            {
                SubmissionId = x.SubmissionId,
                Key = x.Key,
                Ordinal = x.Ordinal,
                Supplier = x.Supplier,
                PurchaseDate = x.PurchaseDate?.UtcDateTime,
                Purpose = x.Purpose,
                ContainsPersonalItems = x.ContainsPersonalItems,
                ReceiptIsItemised = x.ReceiptIsItemised,
                TotalIncGst = x.TotalIncGst,
                GstAmount = x.GstAmount,
                NonReimbursedAmount = x.NonReimbursedAmount,
                Items = x.Items.OrderBy(item => item.Ordinal).Select(item => new ExpenseDetailItem
                {
                    DetailId = item.DetailId,
                    Ordinal = item.Ordinal,
                    Description = item.Description,
                    Amount = item.Amount,
                    IsChurchUse = item.IsChurchUse
                }).ToList()
            })
            .ToList();

        (decimal gross, decimal gst, decimal lessPersonal) = ExpenseTotals.SumDetails(details);
        decimal net = ExpenseTotals.Net(gross, lessPersonal);

        List<string> errors = ValidateForSubmit(submission, details, gross);

        if (errors.Count > 0)
            return SubmitExpenseSubmissionResponse.WithErrors(errors);

        submission.GrossTotal = gross;
        submission.GstTotal = gst;
        submission.LessPersonalAmount = lessPersonal;
        submission.NetTotal = net;
        submission.Status = SubmissionStatus.Submitted;
        submission.SubmittedAt = DateTimeOffset.UtcNow;
        submission.SignedAt = DateTimeOffset.UtcNow;

        // Marking the properties this method owns rather than calling db.Update, which writes every
        // column and would silently revert anything another writer committed since the read above.
        db.Entry(submission).Property(x => x.GrossTotal).IsModified = true;
        db.Entry(submission).Property(x => x.GstTotal).IsModified = true;
        db.Entry(submission).Property(x => x.LessPersonalAmount).IsModified = true;
        db.Entry(submission).Property(x => x.NetTotal).IsModified = true;
        db.Entry(submission).Property(x => x.Status).IsModified = true;
        db.Entry(submission).Property(x => x.SubmittedAt).IsModified = true;
        db.Entry(submission).Property(x => x.SignedAt).IsModified = true;
        db.Entry(submission).Property(x => x.Reference).IsModified = true;

        await SaveWithAReferenceAsync(submission, token);

        return new SubmitExpenseSubmissionResponse
        {
            Success = true,
            Reference = submission.Reference
        };
    }

    /// <summary>
    /// Writes the submitted row, issuing the claim reference and taking the next one if this one has
    /// been taken in the meantime.
    /// </summary>
    /// <remarks>
    /// <b>The retry is the point, and the unique index is what makes it work.</b> The number is chosen
    /// by reading the highest one already issued this year, which is a read followed by a write with a
    /// gap in the middle: two claims submitted in the same second both read 141 and both try to be
    /// 0142. Without the constraint the second silently duplicates the first, and two claims that share
    /// a reference is precisely the failure the reference exists to prevent - a reviewer matching a bank
    /// line would have no way to tell which claim was meant.
    /// <para>
    /// A sequence in the database would remove the loop, and was not worth a migration for it: this
    /// form sees a few dozen claims a year, so the contention is theoretical, and a sequence hands out
    /// numbers to attempts that then fail validation - leaving permanent holes somebody eventually has
    /// to explain. The loop only consumes a number when the row is actually written.
    /// </para>
    /// </remarks>
    private async Task SaveWithAReferenceAsync(DbExpenseSubmission submission, CancellationToken token)
    {
        int year = (submission.SubmittedAt ?? DateTimeOffset.UtcNow).ToLocalTime().Year;

        for (int attempt = 0; ; attempt++)
        {
            submission.Reference = await NextReferenceAsync(submission.Kind, year, token);

            try
            {
                await db.SaveChangesAsync(token);

                return;
            }
            // Anything other than the reference colliding is a real fault and has to surface. Five
            // attempts is a ceiling rather than an expectation: a sixth collision means something is
            // wrong that retrying will not fix.
            catch (DbUpdateException ex) when (IsReferenceCollision(ex) && attempt < 4)
            {
                db.Entry(submission).Property(x => x.Reference).CurrentValue = null;
            }
        }
    }

    /// <summary>
    /// The next reference for a kind and a year: <c>RE-2026-0142</c>, or <c>DC-2026-0087</c>.
    /// </summary>
    /// <remarks>
    /// Two prefixes rather than one running number, because the two are different documents and a
    /// finance reviewer holding one knows from the reference alone which form to expect. The year is
    /// LOCAL rather than UTC - a claim filed on the evening of 31 December in Brisbane belongs to that
    /// year to everybody who will ever look at it.
    /// <para>
    /// <c>IgnoreQueryFilters</c> so a soft-deleted claim's number is not handed out again. Nothing here
    /// hard-deletes, and a reference that appears twice in seven years of records - once on a voided
    /// claim and once on a live one - is worse than a gap in the sequence.
    /// </para>
    /// </remarks>
    private async Task<string> NextReferenceAsync(SubmissionKind kind, int year, CancellationToken token)
    {
        string prefix = kind == SubmissionKind.DebitCardPurchase ? "DC" : "RE";
        string stem = $"{prefix}-{year}-";

        List<string> issued = await db.ExpenseSubmissions
            .IgnoreQueryFilters()
            .Where(x => x.Reference != null && x.Reference.StartsWith(stem))
            .Select(x => x.Reference!)
            .ToListAsync(token);

        int highest = issued
            .Select(x => int.TryParse(x[stem.Length..], out int number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        // Four digits, and it grows rather than wrapping if a year ever needs a five-digit claim. The
        // column is 16 characters, so there is room.
        return $"{stem}{highest + 1:0000}";
    }

    /// <summary>
    /// Whether a failed write was the reference's unique index rather than anything else.
    /// </summary>
    /// <remarks>
    /// Matched on the constraint name, which EF builds from the table and column and which the
    /// migration therefore fixes. Matching on the Postgres SQLSTATE alone (23505) would catch the
    /// attachment content-hash index too, and retrying that one would loop forever.
    /// </remarks>
    private static bool IsReferenceCollision(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: "23505" } postgres
        && postgres.ConstraintName?.Contains("Reference", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Every completeness rule the form has. <paramref name="details"/> is the stored section 3 read
    /// back through the contract, so the itemisation rules are evaluated against the same
    /// <see cref="ExpenseDetail.Itemisation"/> the page used to decide what to ask for.
    /// </summary>
    private static List<string> ValidateForSubmit(
        DbExpenseSubmission submission,
        IReadOnlyList<ExpenseDetail> details,
        decimal gross
    )
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(submission.SubmitterName))
            errors.Add(NeedsASubmitterName);

        if (string.IsNullOrWhiteSpace(submission.PurposeNarrative))
            errors.Add(NeedsAPurposeNarrative);

        // Section 3 has to say what was bought. A draft is allowed to have no details at all - somebody
        // fills section 1 in first - so this is asked once, here, and never while they are typing.
        if (details.Count == 0)
            errors.Add(SubmissionNeedsADetail);

        // Section 3 is now the same shape on both forms, which is the one place this rewrite made the
        // app simpler rather than richer. The old table's first column was a DIFFERENT FIELD on each
        // document - an item description on the card form, a date on the reimbursement one - and needed
        // a switch on the kind right here. A receipt has a supplier and a date on both.
        bool hasUnreceiptedPurchase = false;

        foreach (ExpenseDetail detail in details)
        {
            List<DbExpenseAttachment> files = submission.Attachments
                .Where(x => x.DetailKey == detail.Key)
                .ToList();

            // A detail exists because a file was attached to it, so an empty one means the claimant
            // removed the last file and left the panel behind.
            if (files.Count == 0)
                errors.Add(DetailNeedsAnAttachment);
            else if (files.All(x => x.Kind != AttachmentKind.SupplierReceipt))
                hasUnreceiptedPurchase = true;

            if (string.IsNullOrWhiteSpace(detail.Supplier))
                errors.Add(DetailNeedsASupplier);

            if (detail.PurchaseDate is null)
                errors.Add(DetailNeedsAPurchaseDate);

            if (string.IsNullOrWhiteSpace(detail.Purpose))
                errors.Add(DetailNeedsAPurpose);

            if (detail.TotalIncGst <= 0)
                errors.Add(DetailNeedsATotal);

            // Both, and separately from the itemisation rules below: what those rules ARE is decided by
            // these two answers, so an unanswered pair is not a receipt that needs no itemising - it is
            // a receipt nobody has said anything about yet.
            if (detail.ContainsPersonalItems is null || detail.ReceiptIsItemised is null)
                errors.Add(DetailQuestionsUnanswered);

            if (detail.Itemisation != ItemisationRequirement.None)
            {
                if (detail.Items.Count == 0)
                    errors.Add(DetailNeedsItemisation);

                if (detail.Items.Any(x => string.IsNullOrWhiteSpace(x.Description)))
                    errors.Add(ItemNeedsADescription);

                // Only meaningful in the everything-itemised mode, where the church/personal toggle is
                // on screen. In the personal-items-only mode every stored item is already personal -
                // Create forces IsChurchUse false there - so the condition cannot fire.
                if (detail.ContainsPersonalItems == true && detail.Items.Count > 0
                    && detail.Items.All(x => x.IsChurchUse))
                {
                    errors.Add(PersonalItemsNeedListing);
                }
            }

            // THE FLOOR, and it is only checked here. A claimant halfway through typing the personal
            // lines has itemised $12 of an eventual $40, and a DRAFT refused for that is a draft that
            // goes unsaved while somebody is still working on it - so Create lets it through.
            //
            // Above the floor is deliberately fine. Somebody choosing to carry more of a legitimate cost
            // than they have to is making a gift, and the form has no business refusing one.
            if (ExpenseTotals.Money(detail.NonReimbursedAmount) < ExpenseTotals.PersonalItemsTotal(detail))
                errors.Add(NonReimbursedBelowPersonalItems);
        }

        // NOT "the itemised lines have to add up to the receipt total". Where the evidence does not
        // itemise, a claimant reading a faded thermal docket is asked for best effort, and a form that
        // refuses to submit until the cents reconcile is a form that gets a made-up line added to it to
        // close the gap. The page shows the difference; a reviewer can see it and ask.

        // Section 5: a purchase evidenced only by a bank line or a screenshot. That proves the money
        // moved and says nothing about what it bought, which is exactly the gap the declaration covers.
        if (hasUnreceiptedPurchase && submission.MissingReceipt is not { Declared: true })
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
                        $"Section 1: the card was charged {Money(charged)}, but the receipts in "
                        + $"section 3 total {Money(gross)}. Attach the missing receipt, or correct "
                        + "the amount charged.");
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

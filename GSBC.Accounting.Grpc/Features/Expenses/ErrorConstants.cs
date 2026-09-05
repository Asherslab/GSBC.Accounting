namespace GSBC.Accounting.Grpc.Features.Expenses;

/// <summary>
/// The error strings the expense services answer with, in one place so the page and the service cannot
/// disagree about the wording.
/// </summary>
/// <remarks>
/// These are written to be read by a volunteer filling in a form, not by a developer. "Refused" here
/// always means the submission is incomplete or internally inconsistent - never that the app has formed
/// a view on whether an expense was legitimate.
/// <para>
/// <b>Every submit-time message leads with its section, and that is load-bearing rather than a
/// style.</b> The claimant is reading a list of eight of these on a phone, under a form with eight
/// sections, and the first thing each item has to answer is "where does this send me". It is also what
/// the page's links are built from: <c>SubmissionProblem</c> carries the anchor beside the message, so
/// the two cannot drift, and a message that stated its section only in prose would be a message the
/// banner could not link.
/// </para>
/// </remarks>
public static class ErrorConstants
{
    public const string DetailTotalCannotBeNegative =
        "A receipt's total cannot be negative.";

    public const string GstCannotExceedTotal =
        "A receipt's GST cannot be more than its total.";

    public const string ItemAmountCannotBeNegative =
        "An itemised amount cannot be negative.";

    public const string NonReimbursedCannotBeNegative =
        "The unclaimed amount cannot be negative.";

    public const string NonReimbursedCannotExceedTotal =
        "The unclaimed amount cannot exceed the receipt total.";

    // Half-typed, not wrong: "12" is where "1234" passes through on the way in. The draft rule is only
    // that what is stored cannot BE a card number - digits, and no more than four of them.
    public const string CardLastFourDigitsMustBeDigitsOnly =
        "Card last 4 digits must be digits only, and no more than four. Never record the full card number.";

    // ---- Submit only. A draft is allowed to be half-finished; a submission is not. ----

    public const string CardLastFourDigitsMustBeFourDigits =
        "Section 1: the card's last 4 digits must be exactly four digits. Never record the full card "
        + "number.";

    public const string SubmissionNeedsADetail =
        "Section 3: at least one receipt is required.";

    public const string DetailNeedsAnAttachment =
        "Section 3: every purchase requires at least one attached file.";

    public const string DetailNeedsASupplier =
        "Section 3: every purchase requires a supplier.";

    public const string DetailNeedsAPurchaseDate =
        "Section 3: every purchase requires a date of purchase.";

    public const string DetailNeedsAPurpose =
        "Section 3: every purchase requires a Church purpose.";

    public const string DetailNeedsATotal =
        "Section 3: every purchase requires a receipt total.";

    public const string DetailQuestionsUnanswered =
        "Section 3: every purchase must state whether it includes personal items and whether the "
        + "receipt is itemised. A blank answer is not recorded as No.";

    public const string DetailNeedsItemisation =
        "Section 3: a purchase requires itemisation and has no items listed. An itemised receipt needs "
        + "only its personal lines; evidence that is not itemised needs every item on it.";

    public const string ItemNeedsADescription =
        "Section 3: every itemised line requires a description.";

    public const string PersonalItemsNeedListing =
        "Section 3: a purchase states that it includes personal items, but no itemised line is marked "
        + "as one.";

    public const string NonReimbursedBelowPersonalItems =
        "Section 3: the unclaimed amount is less than the personal items listed on that receipt. A "
        + "larger amount is accepted - that is a gift to the Church - but not a smaller one.";

    public const string SubmissionNotFound =
        "That submission could not be found.";

    public const string AlreadySubmitted =
        "This form has already been submitted.";

    public const string NeedsASubmitterName =
        "Section 1: enter the claimant's name.";

    public const string NeedsAPurposeNarrative =
        "Section 2: an explanation of the Church purpose is required.";

    public const string MissingEvidenceNeedsADeclaration =
        "Section 5: a purchase has no supplier receipt, so the missing receipt declaration must be "
        + "completed and agreed.";

    public const string ComplianceQuestionsUnanswered =
        "Section 4: every question must be answered Yes or No. A blank answer is not recorded as No.";

    public const string DeclarationsNotAgreed =
        "Section 6: all declarations must be agreed before the form can be submitted.";

    public const string NeedsASignature =
        "Section 6: enter your name as signature.";

    public const string DebitCardNeedsCardLastFour =
        "Section 1: enter the last four digits of the card.";

    public const string DebitCardNeedsAmountCharged =
        "Section 1: enter the amount the card was charged.";
}

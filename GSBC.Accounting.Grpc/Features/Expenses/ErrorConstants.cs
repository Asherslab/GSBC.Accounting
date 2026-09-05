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

    // ---- Per purchase, and each one names the purchase it is about. ----
    //
    // These were constants reading "every purchase in section 3 needs...", which is a true sentence
    // that is no use to somebody with six receipts attached: it does not say which of the six. The
    // ordinal is the same number the panel shows in its header and the same one the PDF prints in its
    // evidence manifest, so a claimant reading the refusal, a claimant looking at the form and a
    // reviewer holding a printout are all counting the same way.

    public static string DetailNeedsAnAttachment(int purchase) =>
        $"Section 3, purchase {purchase}: at least one attached file is required.";

    public static string DetailNeedsASupplier(int purchase) =>
        $"Section 3, purchase {purchase}: a supplier is required.";

    public static string DetailNeedsAPurchaseDate(int purchase) =>
        $"Section 3, purchase {purchase}: a date of purchase is required.";

    public static string DetailNeedsAPurpose(int purchase) =>
        $"Section 3, purchase {purchase}: a Church purpose is required.";

    public static string DetailNeedsATotal(int purchase) =>
        $"Section 3, purchase {purchase}: a receipt total is required.";

    public static string DetailQuestionsUnanswered(int purchase) =>
        $"Section 3, purchase {purchase}: state whether it includes personal items and whether the "
        + "receipt is itemised. A blank answer is not recorded as No.";

    public static string DetailNeedsItemisation(int purchase) =>
        $"Section 3, purchase {purchase}: itemisation is required and no items are listed. An itemised "
        + "receipt needs only its personal lines; evidence that is not itemised needs every item on it.";

    public static string ItemNeedsADescription(int purchase) =>
        $"Section 3, purchase {purchase}: every itemised line requires a description.";

    public static string PersonalItemsNeedListing(int purchase) =>
        $"Section 3, purchase {purchase}: it states that it includes personal items, but no itemised "
        + "line is marked as one.";

    public static string NonReimbursedBelowPersonalItems(int purchase) =>
        $"Section 3, purchase {purchase}: the unclaimed amount is less than the personal items listed "
        + "on it. A larger amount is accepted - that is a gift to the Church - but not a smaller one.";

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

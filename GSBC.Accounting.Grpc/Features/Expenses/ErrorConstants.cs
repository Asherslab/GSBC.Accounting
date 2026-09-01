namespace GSBC.Accounting.Grpc.Features.Expenses;

/// <summary>
/// The error strings the expense services answer with, in one place so the page and the service cannot
/// disagree about the wording.
/// </summary>
/// <remarks>
/// These are written to be read by a volunteer filling in a form, not by a developer. "Refused" here
/// always means the submission is incomplete or internally inconsistent - never that the app has formed
/// a view on whether an expense was legitimate.
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
        "The amount you are not claiming cannot be negative.";

    public const string NonReimbursedCannotExceedTotal =
        "The amount you are not claiming cannot be more than the receipt's total.";

    // Half-typed, not wrong: "12" is where "1234" passes through on the way in. The draft rule is only
    // that what is stored cannot BE a card number - digits, and no more than four of them.
    public const string CardLastFourDigitsMustBeDigitsOnly =
        "Card last 4 digits must be digits only, and no more than four. Never record the full card number.";

    // ---- Submit only. A draft is allowed to be half-finished; a submission is not. ----

    public const string CardLastFourDigitsMustBeFourDigits =
        "Card last 4 digits must be exactly four digits. Never record the full card number.";

    public const string SubmissionNeedsADetail =
        "Attach at least one receipt in section 3 - the form needs to say what was bought.";

    public const string DetailNeedsAnAttachment =
        "Every purchase in section 3 needs at least one file attached to it.";

    public const string DetailNeedsASupplier =
        "Every purchase in section 3 needs the place it was bought.";

    public const string DetailNeedsAPurchaseDate =
        "Every purchase in section 3 needs the date it was made.";

    public const string DetailNeedsAPurpose =
        "Every purchase in section 3 needs the Church purpose it was for.";

    public const string DetailNeedsATotal =
        "Every purchase in section 3 needs the total on the receipt.";

    public const string DetailQuestionsUnanswered =
        "Every purchase in section 3 has to say whether it includes personal items and whether the "
        + "receipt is itemised. Leaving one blank is not the same as answering No.";

    public const string DetailNeedsItemisation =
        "A purchase in section 3 needs itemising and has no items listed. Personal items on an itemised "
        + "receipt need only the personal lines; evidence that is not itemised needs everything on it.";

    public const string ItemNeedsADescription =
        "Every itemised line in section 3 needs a description of what it was.";

    public const string PersonalItemsNeedListing =
        "A purchase says it includes personal items, but none of the itemised lines is marked as one.";

    public const string NonReimbursedBelowPersonalItems =
        "The amount you are not claiming is less than the personal items you listed on that receipt. It "
        + "can be more - that is a gift to the Church - but it cannot be less.";

    public const string SubmissionNotFound =
        "That submission could not be found.";

    public const string AlreadySubmitted =
        "This form has already been submitted.";

    public const string NeedsASubmitterName =
        "Say who is making this claim.";

    public const string NeedsAPurposeNarrative =
        "Section 2 needs the written explanation of the Church purpose.";

    public const string MissingEvidenceNeedsADeclaration =
        "A purchase in section 3 has no receipt from the place it was bought, so section 5's missing "
        + "receipt declaration must be completed and agreed.";

    public const string ComplianceQuestionsUnanswered =
        "Every question in section 4 needs a No or a Yes. Leaving one blank is not the same as answering No.";

    public const string DeclarationsNotAgreed =
        "All of the declarations in section 6 have to be agreed before the form can be submitted.";

    public const string NeedsASignature =
        "Type your name against the declarations in section 6.";

    public const string DebitCardNeedsCardLastFour =
        "Section 1 needs the last four digits of the card.";

    public const string DebitCardNeedsAmountCharged =
        "Section 1 needs the amount the card was charged.";
}

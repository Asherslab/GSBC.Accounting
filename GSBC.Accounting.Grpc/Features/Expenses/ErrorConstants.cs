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
    public const string SubmissionNeedsALine =
        "Add at least one line to section 3 - the form needs to say what was bought.";

    public const string LineNeedsAGrossAmount =
        "Every line in section 3 needs a gross amount.";

    public const string GrossAmountCannotBeNegative =
        "A line's gross amount cannot be negative.";

    public const string GstCannotExceedGross =
        "A line's GST cannot be more than its gross amount.";

    public const string ChurchUsePercentOutOfRange =
        "Church use % must be between 0 and 100.";

    public const string LessPersonalCannotBeNegative =
        "The personal portion cannot be negative.";

    public const string CardLastFourDigitsMustBeFourDigits =
        "Card last 4 digits must be exactly four digits. Never record the full card number.";

    public const string DebitCardLineNeedsAnItem =
        "Every line on a debit card form needs an item description.";

    public const string ReimbursementLineNeedsADate =
        "Every line on a reimbursement form needs a date.";

    // ---- Submit only. A draft is allowed to be half-finished; a submission is not. ----

    public const string SubmissionNotFound =
        "That submission could not be found.";

    public const string AlreadySubmitted =
        "This form has already been submitted.";

    public const string NeedsASubmitterName =
        "Say who is making this claim.";

    public const string NeedsAPurposeNarrative =
        "Section 2 needs the written explanation of the Church purpose.";

    public const string NeedsEvidence =
        "Attach at least one itemised receipt or tax invoice. If evidence genuinely cannot be obtained, "
        + "mark the line Missing in section 3 and complete the missing receipt declaration.";

    public const string MissingEvidenceNeedsADeclaration =
        "A line is marked Missing, so section 5's missing receipt declaration must be completed and agreed.";

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

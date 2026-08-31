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
}

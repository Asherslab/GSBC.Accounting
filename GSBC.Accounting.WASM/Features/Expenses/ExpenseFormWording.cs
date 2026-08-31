using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Every word the two forms print, keyed by kind. <b>Quoted from the `.docx` files, not paraphrased.</b>
/// </summary>
/// <remarks>
/// This class exists because the two forms share a structure and almost none of their wording. Measured
/// against the source documents: two of the six compliance questions are word-for-word identical, one of
/// the five declarations, and none of section 5. Sections 2 and 3 differ too.
/// <para>
/// <b>Do not collapse two nearly-identical strings into one shared constant.</b> On a compliance
/// document the exact words are the content - a form that asks a subtly different question has recorded
/// an answer to a question nobody asked. Where the two forms genuinely agree the same literal appears
/// twice below, deliberately, so changing one never changes the other by accident.
/// </para>
/// <para>
/// Even the apostrophes differ: the debit card form uses ASCII <c>'</c> in "Church's" and the
/// reimbursement form uses <c>’</c>. Each is reproduced as printed.
/// </para>
/// </remarks>
public static class ExpenseFormWording
{
    public static ExpenseFormText For(SubmissionKind kind) =>
        kind == SubmissionKind.DebitCardPurchase ? DebitCard : Reimbursement;

    private static readonly ExpenseFormText DebitCard = new()
    {
        PageTitle = "Debit Card Purchase",
        DocumentTitle = "Church Debit Card Purchase Form",
        DocumentSubtitle = "For recording and substantiating every purchase made using a Church debit card",
        Banner = "Complete one form for each transaction. Attach the itemised receipt/tax invoice and "
                 + "submit promptly. A card terminal receipt or bank statement alone does not show what "
                 + "was purchased.",

        Section1Caption = "Cardholder and transaction details",
        Section1Hint = "Complete all applicable fields",
        SubmitterNameLabel = "Cardholder name",
        FormDateLabel = "Form date",

        Section2Caption = "Church purpose and authorisation",
        PurposeActivityLabel = "Ministry / department",
        PurposeNarrativePrompt = "What was purchased, who used or benefited from it, and how did it "
                                 + "further the Church's charitable/religious purposes?",

        Section3Caption = "Purchase and evidence details",
        Section3Hint = "Itemise the complete card transaction",
        LineColumn1Header = "Item",
        LineColumn2Header = "Qty / details",
        LineColumn3Header = "Church purpose / user",

        GrossTotalLabel = "Total card transaction",
        LessPersonalLabel = "Less personal portion to be repaid immediately",
        NetTotalLabel = "Net authorised church expense",

        SignatureLabel = "Cardholder signature (confirming all declarations on page 2)"
    };

    private static readonly ExpenseFormText Reimbursement = new()
    {
        PageTitle = "Expense Reimbursement",
        DocumentTitle = "Expense Reimbursement Form",
        DocumentSubtitle = "For expenses personally paid on behalf of Good Shepherd Baptist Church",
        Banner = "Attach itemised receipts/tax invoices. Submit promptly and do not approve your own "
                 + "claim. A card receipt or bank statement alone does not show what was purchased.",

        Section1Caption = "Claimant and payment details",
        Section1Hint = "Complete all applicable fields",
        SubmitterNameLabel = "Claimant name",
        FormDateLabel = "Claim date",

        Section2Caption = "Business purpose and authorisation",
        PurposeActivityLabel = "Purpose / activity",
        PurposeNarrativePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",

        Section3Caption = "Expense details",
        Section3Hint = "Use one line per receipt / transaction",
        LineColumn1Header = "Date",
        LineColumn2Header = "Supplier & item / service",
        LineColumn3Header = "Purpose / ministry",

        GrossTotalLabel = "Subtotal of receipts",
        LessPersonalLabel = "Less personal / non-reimbursable portion",
        NetTotalLabel = "Total reimbursement claimed",

        SignatureLabel = "Claimant signature (confirming all declarations on page 2)"
    };
}

public record ExpenseFormText
{
    public required string PageTitle { get; init; }
    public required string DocumentTitle { get; init; }
    public required string DocumentSubtitle { get; init; }
    public required string Banner { get; init; }

    public required string Section1Caption { get; init; }
    public required string Section1Hint { get; init; }
    public required string SubmitterNameLabel { get; init; }
    public required string FormDateLabel { get; init; }

    public required string Section2Caption { get; init; }
    public required string PurposeActivityLabel { get; init; }
    public required string PurposeNarrativePrompt { get; init; }

    public required string Section3Caption { get; init; }
    public required string Section3Hint { get; init; }
    public required string LineColumn1Header { get; init; }
    public required string LineColumn2Header { get; init; }
    public required string LineColumn3Header { get; init; }

    public required string GrossTotalLabel { get; init; }
    public required string LessPersonalLabel { get; init; }
    public required string NetTotalLabel { get; init; }

    public required string SignatureLabel { get; init; }
}

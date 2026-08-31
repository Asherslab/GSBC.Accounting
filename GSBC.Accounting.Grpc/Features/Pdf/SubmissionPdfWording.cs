using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// Every word the printed form carries, keyed by kind.
/// </summary>
/// <remarks>
/// <b>A deliberate second copy of the WASM project's <c>ExpenseFormWording</c>, not a shared library.</b>
/// The two could be one assembly, and that would be the wrong trade: the screen and the printed
/// document are allowed to diverge, and a shared string means a change made for the page silently
/// rewrites what a claimant signed in a document already filed for seven years.
/// <para>
/// Both are quoted from the same <c>.docx</c> files, so they should agree today. If they ever disagree,
/// check both against the source documents rather than making one defer to the other.
/// </para>
/// </remarks>
public static class SubmissionPdfWording
{
    public static PdfText For(SubmissionKind kind) => kind == SubmissionKind.DebitCardPurchase ? DebitCard : Reimbursement;

    private static readonly PdfText DebitCard = new()
    {
        DocumentTitle = "CHURCH DEBIT CARD PURCHASE FORM",
        DocumentSubtitle = "For recording and substantiating every purchase made using a Church debit card",
        Section1Caption = "1. CARDHOLDER AND TRANSACTION DETAILS",
        SubmitterNameLabel = "Cardholder name",
        FormDateLabel = "Form date",
        Section2Caption = "2. CHURCH PURPOSE AND AUTHORISATION",
        PurposeActivityLabel = "Ministry / department",
        PurposeNarrativePrompt = "What was purchased, who used or benefited from it, and how did it further "
                                 + "the Church's charitable/religious purposes?",
        Section3Caption = "3. PURCHASE AND EVIDENCE DETAILS",
        LineColumn1Header = "Item",
        LineColumn2Header = "Qty / details",
        LineColumn3Header = "Church purpose / user",
        GrossTotalLabel = "Total card transaction",
        LessPersonalLabel = "Less personal portion to be repaid immediately",
        NetTotalLabel = "NET AUTHORISED CHURCH EXPENSE",
        Section4Caption = "4. SPECIAL PURCHASE DETAILS AND COMPLIANCE CHECKS",
        ComplianceQuestions =
        [
            "Parking, toll, fuel, taxi or other travel?",
            "Meal, restaurant, catering, gift or hospitality?",
            "Did a spouse, child, family member or private companion attend, travel, dine or benefit?",
            "Expense incurred outside Australia or for an overseas activity?",
            "Cardholder, supplier or recipient is a Responsible Person, senior manager, close family member, or related entity?",
            "Actual, potential or perceived conflict of interest?"
        ],
        DetailTableCaption = "Meals / hospitality / gifts / travel details",
        ComplianceDetailsCaption = "Conflict / related-party / personal repayment / overseas details",
        MissingReceiptDeclaration =
            "I declare that the card charge was made for the stated Church purpose, the details are accurate, "
            + "and I have supplied all available evidence. I understand GST must not be claimed unless the "
            + "Church holds the evidence required by law.",
        Section6Caption = "6. CARDHOLDER DECLARATION",
        Declarations =
        [
            "STRICTLY CHURCH EXPENSE: Every amount treated as a Church expense was incurred solely for an "
            + "authorised Church purpose. No personal, private or family expense is included.",
            "Where my spouse, child, family member or another private companion attended, accompanied me or "
            + "received any benefit, I have separately identified and excluded all of their costs and their "
            + "fair share of every joint or shared expense (including meals, travel, accommodation, tickets "
            + "and transport).",
            "If any personal or unauthorised amount was inadvertently charged, I have disclosed it and repaid "
            + "or arranged immediate repayment to the Church. I understand the Church debit card must not be "
            + "used for personal purchases.",
            "The attached evidence is genuine and itemised. I have disclosed discounts, refunds, credits, "
            + "loyalty benefits used as payment, insurance recoveries and any private use.",
            "I have identified meal attendees, gift recipients and beneficiaries where applicable, and "
            + "disclosed any conflict of interest or related-party connection."
        ],
        SignatureLabel = "Cardholder signature (confirming all declarations on page 2)",
        Section8Caption = "8. FINANCE USE ONLY",
        FinanceFields = ["Transaction reference", "Statement date", "Personal repayment"],
        ApprovedAmountLabel = "Church expense approved",
        ThirdDecisionLabel = "Repayment required",
        Footer = "Debit Card Purchase Form  |  Version 1.0 - August 2026  |  "
                 + "Retain with supporting records for at least 7 years"
    };

    private static readonly PdfText Reimbursement = new()
    {
        DocumentTitle = "EXPENSE REIMBURSEMENT FORM",
        DocumentSubtitle = "For expenses personally paid on behalf of Good Shepherd Baptist Church",
        Section1Caption = "1. CLAIMANT AND PAYMENT DETAILS",
        SubmitterNameLabel = "Claimant name",
        FormDateLabel = "Claim date",
        Section2Caption = "2. BUSINESS PURPOSE AND AUTHORISATION",
        PurposeActivityLabel = "Purpose / activity",
        PurposeNarrativePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",
        Section3Caption = "3. EXPENSE DETAILS",
        LineColumn1Header = "Date",
        LineColumn2Header = "Supplier & item / service",
        LineColumn3Header = "Purpose / ministry",
        GrossTotalLabel = "Subtotal of receipts",
        LessPersonalLabel = "Less personal / non-reimbursable portion",
        NetTotalLabel = "TOTAL REIMBURSEMENT CLAIMED",
        Section4Caption = "4. SPECIAL CATEGORIES AND COMPLIANCE CHECKS",
        ComplianceQuestions =
        [
            "Motor vehicle travel claimed?",
            "Entertainment, meals, gifts or hospitality?",
            "Did a spouse, child, family member or other private companion benefit from or accompany the claimant?",
            "Expense incurred outside Australia or for an overseas activity?",
            "Claimant is a Responsible Person, senior manager, close family member, or related entity?",
            "Actual, potential or perceived conflict of interest?"
        ],
        DetailTableCaption = "Motor vehicle trip record",
        ComplianceDetailsCaption = "Conflict / related-party / overseas details",
        MissingReceiptDeclaration =
            "I declare that I paid this amount for the stated Church purpose, have not been and will not be "
            + "reimbursed from another source, and have supplied all available evidence. I understand GST "
            + "must not be claimed unless the Church holds the evidence required by law.",
        Section6Caption = "6. CLAIMANT DECLARATION",
        Declarations =
        [
            "STRICTLY CHURCH EXPENSE: Every amount claimed was incurred and paid by me solely for an "
            + "authorised Church purpose. No personal, private or family expense is included.",
            "Where my spouse, child, family member or another private companion accompanied me or received "
            + "any benefit, I have excluded all of their costs and their fair share of every joint or shared "
            + "expense (including travel, accommodation, meals, tickets and transport).",
            "I have not previously claimed, been reimbursed for, or received an allowance or other payment "
            + "covering these amounts, and I will notify the Church if that changes.",
            "The attached evidence is genuine and itemised. I have disclosed discounts, refunds, credits, "
            + "loyalty benefits used as payment, insurance recoveries and any private use.",
            "I have disclosed any actual, potential or perceived conflict of interest and any related-party "
            + "connection relevant to this claim."
        ],
        SignatureLabel = "Claimant signature (confirming all declarations on page 2)",
        Section8Caption = "8. FINANCE USE ONLY",
        FinanceFields = ["Claim reference", "Payment date", "Payment reference"],
        ApprovedAmountLabel = "Approved for",
        ThirdDecisionLabel = "Returned for information",
        Footer = "Expense Reimbursement Form  |  Version 1.0 - August 2026  |  "
                 + "Retain with supporting records for at least 7 years"
    };
}

public record PdfText
{
    public required string DocumentTitle { get; init; }
    public required string DocumentSubtitle { get; init; }
    public required string Section1Caption { get; init; }
    public required string SubmitterNameLabel { get; init; }
    public required string FormDateLabel { get; init; }
    public required string Section2Caption { get; init; }
    public required string PurposeActivityLabel { get; init; }
    public required string PurposeNarrativePrompt { get; init; }
    public required string Section3Caption { get; init; }
    public required string LineColumn1Header { get; init; }
    public required string LineColumn2Header { get; init; }
    public required string LineColumn3Header { get; init; }
    public required string GrossTotalLabel { get; init; }
    public required string LessPersonalLabel { get; init; }
    public required string NetTotalLabel { get; init; }
    public required string Section4Caption { get; init; }
    public required IReadOnlyList<string> ComplianceQuestions { get; init; }
    public required string DetailTableCaption { get; init; }
    public required string ComplianceDetailsCaption { get; init; }
    public required string MissingReceiptDeclaration { get; init; }
    public required string Section6Caption { get; init; }
    public required IReadOnlyList<string> Declarations { get; init; }
    public required string SignatureLabel { get; init; }
    public required string Section8Caption { get; init; }
    public required IReadOnlyList<string> FinanceFields { get; init; }
    public required string ApprovedAmountLabel { get; init; }
    public required string ThirdDecisionLabel { get; init; }
    public required string Footer { get; init; }
}

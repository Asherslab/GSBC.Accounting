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
/// The two should agree today. If they ever disagree, check both against
/// <c>docs/modules/expenses/paper-form-fields.md</c> and the departures from it listed in
/// <c>submission-model.md</c>, rather than making one defer to the other.
/// </para>
/// <para>
/// The section captions are the same on both forms - unified at the accounts team's request on
/// 2026-10-13 - except section 1, which names the person. The same literal still appears twice, so a
/// later change to one form's caption cannot reach the other's by accident.
/// </para>
/// </remarks>
public static class SubmissionPdfWording
{
    public static PdfText For(SubmissionKind kind) => kind == SubmissionKind.DebitCardPurchase ? DebitCard : Reimbursement;

    private static readonly PdfText DebitCard = new()
    {
        DocumentTitle = "CHURCH DEBIT CARD PURCHASE FORM",
        DocumentSubtitle = "For recording and substantiating every purchase made using a Church debit card",
        Section1Caption = "1. CARDHOLDER DETAILS",
        SubmitterNameLabel = "Cardholder name",
        FormDateLabel = "Form date",
        Section2Caption = "2. AUTHORISATION",
        Section3Caption = "3. EXPENSE DETAILS",
        PurposePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",
        GrossTotalLabel = "Total card transaction",
        LessPersonalLabel = "Less personal portion to be repaid immediately",
        NetTotalLabel = "NET AUTHORISED CHURCH EXPENSE",
        Section4Caption = "4. COMPLIANCE QUESTIONS",
        ComplianceQuestions =
        [
            new(0, "Parking, toll, fuel, taxi or other travel?"),
            new(1, "Meal, restaurant, catering, gift or hospitality?"),
            new(2, "Did a spouse, child, family member or private companion attend, travel, dine or benefit?"),
            new(3, "Expense incurred outside Australia or for an overseas activity?"),
            new(4, "Cardholder, supplier or recipient is a Responsible Person, senior manager, close family member, or related entity?"),
            new(5, "Actual, potential or perceived conflict of interest?")
        ],
        DetailTableCaption = "Meals / hospitality / gifts / travel details",
        ComplianceDetailsCaption = "Conflict / related-party / personal repayment / overseas details",
        MissingReceiptDeclaration =
            "I declare that the card charge was made for the stated Church purpose, the details are accurate, "
            + "and I have supplied all available evidence. I understand GST must not be claimed unless the "
            + "Church holds the evidence required by law.",
        Section6Caption = "6. DECLARATIONS",
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
        SignatureLabel = "Cardholder signature (confirming all declarations above)",
        Footer = "Debit Card Purchase Form  |  Version 1.0 - August 2026  |  "
                 + "Retain with supporting records for at least 7 years"
    };

    private static readonly PdfText Reimbursement = new()
    {
        DocumentTitle = "EXPENSE REIMBURSEMENT FORM",
        DocumentSubtitle = "For expenses personally paid on behalf of Good Shepherd Baptist Church",
        Section1Caption = "1. CLAIMANT DETAILS",
        SubmitterNameLabel = "Claimant name",
        FormDateLabel = "Claim date",
        Section2Caption = "2. AUTHORISATION",
        Section3Caption = "3. EXPENSE DETAILS",
        PurposePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",
        GrossTotalLabel = "Subtotal of receipts",
        LessPersonalLabel = "Less personal / non-reimbursable portion",
        NetTotalLabel = "TOTAL REIMBURSEMENT CLAIMED",
        Section4Caption = "4. COMPLIANCE QUESTIONS",
        // Five, not six: the paper form's question 1 (motor vehicle travel) was removed at the accounts
        // team's request. Numbered 1 to 5 as printed; the slots are the stored columns Q2 to Q6.
        ComplianceQuestions =
        [
            new(1, "Entertainment, meals, gifts or hospitality?"),
            new(2, "Did a spouse, child, family member or other private companion benefit from or accompany the claimant?"),
            new(3, "Expense incurred outside Australia or for an overseas activity?"),
            new(4, "Claimant is a Responsible Person, senior manager, close family member, or related entity?"),
            new(5, "Actual, potential or perceived conflict of interest?")
        ],
        // No detail table: the trip record it used to caption went with question 1.
        DetailTableCaption = null,
        ComplianceDetailsCaption = "Conflict / related-party / overseas details",
        MissingReceiptDeclaration =
            "I declare that I paid this amount for the stated Church purpose, have not been and will not be "
            + "reimbursed from another source, and have supplied all available evidence. I understand GST "
            + "must not be claimed unless the Church holds the evidence required by law.",
        Section6Caption = "6. DECLARATIONS",
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
        SignatureLabel = "Claimant signature (confirming all declarations above)",
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
    public required string Section3Caption { get; init; }

    /// <summary>The question asked of every receipt, printed above each one's answer.</summary>
    public required string PurposePrompt { get; init; }

    public required string GrossTotalLabel { get; init; }
    public required string LessPersonalLabel { get; init; }
    public required string NetTotalLabel { get; init; }
    public required string Section4Caption { get; init; }

    /// <summary>The questions this kind asks, in order, each naming the stored answer column it reads.</summary>
    public required IReadOnlyList<PdfQuestion> ComplianceQuestions { get; init; }

    /// <summary>Caption of section 4's attendee table, or null on the form that has none.</summary>
    public required string? DetailTableCaption { get; init; }

    public required string ComplianceDetailsCaption { get; init; }
    public required string MissingReceiptDeclaration { get; init; }
    public required string Section6Caption { get; init; }
    public required IReadOnlyList<string> Declarations { get; init; }
    public required string SignatureLabel { get; init; }
    public required string Footer { get; init; }
}

/// <param name="Slot">Which stored answer this is, 0 to 5 for <c>ComplianceQ1</c> to <c>ComplianceQ6</c>.</param>
/// <param name="Text">The question as printed.</param>
public record PdfQuestion(int Slot, string Text);

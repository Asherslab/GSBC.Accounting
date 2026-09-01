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
        Section1NoticeTitle = "Card security.",
        Section1NoticeBody = "Record only the last four digits. The full card number, PIN or security "
                             + "code must never be entered here or attached in an image.",

        Section2Caption = "Church purpose and authorisation",
        PurposeActivityLabel = "Ministry / department",
        PurposeNarrativePrompt = "What was purchased, who used or benefited from it, and how did it "
                                 + "further the Church's charitable/religious purposes?",
        PurposeNarrativeHint = "Enough detail that someone outside the ministry could see the church "
                               + "purpose without asking.",

        Section3Caption = "Purchase and evidence details",
        Section3Hint = "Itemise the complete card transaction",
        LineColumn1Header = "Item",
        LineColumn2Header = "Qty / details",
        LineColumn3Header = "Church purpose / user",

        GrossTotalLabel = "Total card transaction",
        LessPersonalLabel = "Less personal portion to be repaid immediately",
        NetTotalLabel = "Net authorised church expense",


        Section4Caption = "Special purchase details and compliance checks",
        ComplianceQuestions =
        [
            new("Parking, toll, fuel, taxi or other travel?",
                "record the destination/event and Church purpose, and attach the available receipt or trip evidence."),
            new("Meal, restaurant, catering, gift or hospitality?",
                "list every attendee or recipient, their relationship to the Church, the ministry purpose and any private share below."),
            new("Did a spouse, child, family member or private companion attend, travel, dine or benefit?",
                "identify them and fully exclude their costs and fair share of every joint expense."),
            new("Expense incurred outside Australia or for an overseas activity?",
                "specify country and link to the relevant activity/project records."),
            new("Cardholder, supplier or recipient is a Responsible Person, senior manager, close family member, or related entity?",
                "disclose below and use an independent approver."),
            new("Actual, potential or perceived conflict of interest?",
                "disclose below and record/manage it under the Church conflict-of-interest process.")
        ],
        ComplianceDetailsCaption = "Conflict / related-party / personal repayment / overseas details (if applicable)",
        DetailTableCaption = "Meals / hospitality / gifts / travel details (if applicable)",

        MissingReceiptDeclaration =
            "I declare that the card charge was made for the stated Church purpose, the details are "
            + "accurate, and I have supplied all available evidence. I understand GST must not be claimed "
            + "unless the Church holds the evidence required by law.",

        Section6Caption = "Cardholder declaration",
        Declarations =
        [
            "STRICTLY CHURCH EXPENSE: Every amount treated as a Church expense was incurred solely for an "
            + "authorised Church purpose. No personal, private or family expense is included.",

            "Where my spouse, child, family member or another private companion attended, accompanied me or "
            + "received any benefit, I have separately identified and excluded all of their costs and their "
            + "fair share of every joint or shared expense (including meals, travel, accommodation, tickets "
            + "and transport).",

            "If any personal or unauthorised amount was inadvertently charged, I have disclosed it and "
            + "repaid or arranged immediate repayment to the Church. I understand the Church debit card "
            + "must not be used for personal purchases.",

            "The attached evidence is genuine and itemised. I have disclosed discounts, refunds, credits, "
            + "loyalty benefits used as payment, insurance recoveries and any private use.",

            "I have identified meal attendees, gift recipients and beneficiaries where applicable, and "
            + "disclosed any conflict of interest or related-party connection."
        ],
        SignatureLabel = "Cardholder signature (confirming all declarations on page 2)",

        Footnote = "This form supports compliance but does not replace the Church constitution, "
                   + "delegations, debit card policy, grant conditions, employment obligations, or "
                   + "professional advice. Finance applies the ATO and ACNC requirements current at the "
                   + "payment date."
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
        Section1NoticeTitle = "Banking privacy.",
        Section1NoticeBody = "Do not email bank details in an unsecured message. Use the church-approved "
                             + "secure method. This form never asks for your BSB or account number.",

        Section2Caption = "Business purpose and authorisation",
        PurposeActivityLabel = "Purpose / activity",
        PurposeNarrativePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",
        PurposeNarrativeHint = null,

        Section3Caption = "Expense details",
        Section3Hint = "Use one line per receipt / transaction",
        LineColumn1Header = "Date",
        LineColumn2Header = "Supplier & item / service",
        LineColumn3Header = "Purpose / ministry",

        GrossTotalLabel = "Subtotal of receipts",
        LessPersonalLabel = "Less personal / non-reimbursable portion",
        NetTotalLabel = "Total reimbursement claimed",


        Section4Caption = "Special categories and compliance checks",
        ComplianceQuestions =
        [
            new("Motor vehicle travel claimed?",
                "complete the trip record below. Fuel is not claimed separately where a per-kilometre rate is used."),
            new("Entertainment, meals, gifts or hospitality?",
                "identify attendees, Church purpose and any personal component."),
            new("Did a spouse, child, family member or other private companion benefit from or accompany the claimant?",
                "identify and fully exclude their costs and their share of any joint expense."),
            new("Expense incurred outside Australia or for an overseas activity?",
                "specify country and link to the relevant activity/project records."),
            new("Claimant is a Responsible Person, senior manager, close family member, or related entity?",
                "declare below and use an independent approver."),
            new("Actual, potential or perceived conflict of interest?",
                "disclose below and record/manage it under the Church conflict-of-interest process.")
        ],
        ComplianceDetailsCaption = "Conflict / related-party / overseas details (if applicable)",
        DetailTableCaption = "Motor vehicle trip record (if applicable)",

        MissingReceiptDeclaration =
            "I declare that I paid this amount for the stated Church purpose, have not been and will not be "
            + "reimbursed from another source, and have supplied all available evidence. I understand GST "
            + "must not be claimed unless the Church holds the evidence required by law.",

        Section6Caption = "Claimant declaration",
        Declarations =
        [
            "STRICTLY CHURCH EXPENSE: Every amount claimed was incurred and paid by me solely for an "
            + "authorised Church purpose. No personal, private or family expense is included.",

            "Where my spouse, child, family member or another private companion accompanied me or received "
            + "any benefit, I have excluded all of their costs and their fair share of every joint or "
            + "shared expense (including travel, accommodation, meals, tickets and transport).",

            "I have not previously claimed, been reimbursed for, or received an allowance or other payment "
            + "covering these amounts, and I will notify the Church if that changes.",

            "The attached evidence is genuine and itemised. I have disclosed discounts, refunds, credits, "
            + "loyalty benefits used as payment, insurance recoveries and any private use.",

            "I have disclosed any actual, potential or perceived conflict of interest and any related-party "
            + "connection relevant to this claim."
        ],
        SignatureLabel = "Claimant signature (confirming all declarations on page 2)",

        Footnote = "This form supports compliance but does not replace the Church constitution, "
                   + "delegations, grant conditions, employment obligations, or professional advice. "
                   + "Finance applies the ATO and ACNC requirements current at the payment date."
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

    /// <summary>
    /// The bolded lead of section 1's notice. Paper text, and a different warning on each form: the card
    /// form's is about never writing down more of the card number, the reimbursement form's about how
    /// bank details are sent. Neither belongs on the other's page.
    /// </summary>
    public required string Section1NoticeTitle { get; init; }

    public required string Section1NoticeBody { get; init; }

    public required string Section2Caption { get; init; }
    public required string PurposeActivityLabel { get; init; }
    public required string PurposeNarrativePrompt { get; init; }

    /// <summary>
    /// The help line under the narrative box, or null where the form prints none. Only the debit card
    /// form carries one.
    /// </summary>
    public string? PurposeNarrativeHint { get; init; }

    public required string Section3Caption { get; init; }
    public required string Section3Hint { get; init; }
    public required string LineColumn1Header { get; init; }
    public required string LineColumn2Header { get; init; }
    public required string LineColumn3Header { get; init; }

    public required string GrossTotalLabel { get; init; }
    public required string LessPersonalLabel { get; init; }
    public required string NetTotalLabel { get; init; }

    public required string Section4Caption { get; init; }

    /// <summary>
    /// The six, in order. Only questions 4 and 6 are word-for-word identical between the forms, and
    /// question 1 is a materially different question - incidental travel paid on the card, versus the
    /// claimant's own vehicle at a per-kilometre rate. A shared string would be a compliance error, not
    /// a cosmetic one.
    /// </summary>
    public required IReadOnlyList<ComplianceQuestion> ComplianceQuestions { get; init; }

    public required string ComplianceDetailsCaption { get; init; }

    /// <summary>Caption of the table a Yes on question 1 or 2 opens.</summary>
    public required string DetailTableCaption { get; init; }

    /// <summary>
    /// Section 5's declaration paragraph. Only its closing GST sentence is shared between the forms -
    /// the opening is rewritten, and the reimbursement version adds the no-other-source clause.
    /// </summary>
    public required string MissingReceiptDeclaration { get; init; }

    public required string Section6Caption { get; init; }

    /// <summary>
    /// The five, in order. Only declaration 4 is word-for-word identical, and declaration 3 is a
    /// DIFFERENT declaration on each form - repayment on the debit card, no-double-claim on the
    /// reimbursement. Neither form carries the other's, and closing that gap is finance's decision.
    /// </summary>
    public required IReadOnlyList<string> Declarations { get; init; }

    public required string SignatureLabel { get; init; }

    /// <summary>
    /// The closing disclaimer under section 8. This one is the app's own words rather than the paper
    /// form's, and the two differ only in whether the debit card policy is named - but it is still
    /// per-kind, so the page has no <c>@if</c> on the kind for a line of prose.
    /// </summary>
    public required string Footnote { get; init; }
}

/// <summary>
/// One section 4 question: the text, and the instruction that follows "Yes".
/// </summary>
/// <remarks>
/// Split in two because the page renders them differently - the question beside a No/Yes pair, the
/// instruction inside the panel that a Yes reveals. The paper form prints them as one paragraph.
/// </remarks>
public record ComplianceQuestion(string Question, string YesInstruction);

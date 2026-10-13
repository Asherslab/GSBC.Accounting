using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Every word the two forms print, keyed by kind. The questions and declarations are <b>quoted from the
/// `.docx` files, not paraphrased.</b>
/// </summary>
/// <remarks>
/// This class exists because the two forms share a structure and much less of their wording. Two of the
/// debit card form's six compliance questions are word-for-word identical to the reimbursement form's,
/// one of the five declarations, and none of section 5.
/// <para>
/// <b>Do not collapse two nearly-identical questions or declarations into one shared constant.</b> On a
/// compliance document the exact words are the content - a form that asks a subtly different question
/// has recorded an answer to a question nobody asked. Where the two forms genuinely agree the same
/// literal appears twice below, deliberately, so changing one never changes the other by accident.
/// </para>
/// <para>
/// <b>The section captions are the exception, and on purpose.</b> At the accounts team's request on
/// 2026-10-13 they are the same on both forms, except section 1, which names the person - so the two
/// read as one form with a different first question. They are still written out twice for the reason
/// above.
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

        Section1Caption = "Cardholder details",
        Section1Hint = "Complete all applicable fields",
        SubmitterNameLabel = "Cardholder name",
        FormDateLabel = "Form date",
        Section1NoticeTitle = "Card security.",
        Section1NoticeBody = "Record only the last four digits. The full card number, PIN or security "
                             + "code must never be entered here or attached in an image.",

        Section2Caption = "Authorisation",

        Section3Caption = "Expense details",
        Section3Hint = "One entry per receipt",
        PurposePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",
        PurposeHint = "Enough detail that someone outside the ministry could see the Church purpose "
                      + "without asking.",

        GrossTotalLabel = "Total card transaction",
        LessPersonalLabel = "Less personal portion to be repaid immediately",
        NetTotalLabel = "Net authorised church expense",

        Section4Caption = "Compliance questions",
        ComplianceQuestions =
        [
            new(0, "Parking, toll, fuel, taxi or other travel?",
                "record the destination/event and Church purpose, and attach the available receipt or trip evidence."),
            new(1, "Meal, restaurant, catering, gift or hospitality?",
                "list every attendee or recipient, their relationship to the Church, the ministry purpose and any private share below."),
            new(2, "Did a spouse, child, family member or private companion attend, travel, dine or benefit?",
                "identify them and fully exclude their costs and fair share of every joint expense."),
            new(3, "Expense incurred outside Australia or for an overseas activity?",
                "specify country and link to the relevant activity/project records."),
            new(4, "Cardholder, supplier or recipient is a Responsible Person, senior manager, close family member, or related entity?",
                "disclose below and use an independent approver.",
                "A Responsible Person is someone who governs the Church - a board or committee member, "
                + "elder, deacon or trustee. Answer Yes if that describes you, the business you bought "
                + "from, whoever received the goods, anyone in their immediate family, or a business any "
                + "of them own or control. If unsure, tick No."),
            new(5, "Actual, potential or perceived conflict of interest?",
                "disclose below and record/manage it under the Church conflict-of-interest process.",
                "Yes if someone could reasonably wonder whether you stood to gain personally - for "
                + "example you bought from a relative, a friend, or a business you or your family have "
                + "an interest in. Perceived counts: it applies even where nothing improper happened.")
        ],
        ComplianceDetailsCaption = "Conflict / related-party / personal repayment / overseas details (if applicable)",
        DetailTableCaption = "Meals / hospitality / gifts / travel details (if applicable)",

        MissingReceiptDeclaration =
            "I declare that the card charge was made for the stated Church purpose, the details are "
            + "accurate, and I have supplied all available evidence. I understand GST must not be claimed "
            + "unless the Church holds the evidence required by law.",

        Section6Caption = "Declarations",
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
        SignatureLabel = "Cardholder signature (confirming all declarations above)",

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

        Section1Caption = "Claimant details",
        Section1Hint = "Complete all applicable fields",
        SubmitterNameLabel = "Claimant name",
        FormDateLabel = "Claim date",
        Section1NoticeTitle = "Banking privacy.",
        Section1NoticeBody = "This form never asks for your BSB or account number. If the accounts team "
                             + "does not already have your bank details on file, send them to "
                             + "accounts@baptist.com.au.",

        Section2Caption = "Authorisation",

        Section3Caption = "Expense details",
        Section3Hint = "One entry per receipt",
        PurposePrompt = "How did the expenditure further the Church’s charitable/religious purposes?",
        PurposeHint = "Enough detail that someone outside the ministry could see the Church purpose "
                      + "without asking.",

        GrossTotalLabel = "Subtotal of receipts",
        LessPersonalLabel = "Less personal / non-reimbursable portion",
        NetTotalLabel = "Total reimbursement claimed",

        Section4Caption = "Compliance questions",
        // FIVE, not six. The paper form's question 1 - motor vehicle travel, with its trip record - was
        // removed at the accounts team's request. The rest keep their stored slots, so what the claimant
        // reads as question 1 is answer slot 1, ComplianceQ2.
        ComplianceQuestions =
        [
            new(1, "Entertainment, meals, gifts or hospitality?",
                "identify attendees, Church purpose and any personal component."),
            new(2, "Did a spouse, child, family member or other private companion benefit from or accompany the claimant?",
                "identify and fully exclude their costs and their share of any joint expense."),
            new(3, "Expense incurred outside Australia or for an overseas activity?",
                "specify country and link to the relevant activity/project records."),
            new(4, "Claimant is a Responsible Person, senior manager, close family member, or related entity?",
                "declare below and use an independent approver.",
                "A Responsible Person is someone who governs the Church - a board or committee member, "
                + "elder, deacon or trustee. Answer Yes if that describes you, someone in your immediate "
                + "family, or a business any of you own or control. If unsure, tick No."),
            new(5, "Actual, potential or perceived conflict of interest?",
                "disclose below and record/manage it under the Church conflict-of-interest process.",
                "Yes if someone could reasonably wonder whether you stood to gain personally - for "
                + "example you bought from a relative, a friend, or a business you or your family have "
                + "an interest in. Perceived counts: it applies even where nothing improper happened.")
        ],
        ComplianceDetailsCaption = "Conflict / related-party / overseas details (if applicable)",
        DetailTableCaption = null,

        MissingReceiptDeclaration =
            "I declare that I paid this amount for the stated Church purpose, have not been and will not be "
            + "reimbursed from another source, and have supplied all available evidence. I understand GST "
            + "must not be claimed unless the Church holds the evidence required by law.",

        Section6Caption = "Declarations",
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
        SignatureLabel = "Claimant signature (confirming all declarations above)",

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
    /// The bolded lead of section 1's notice, and a different warning on each form: the card form's is
    /// about never writing down more of the card number, the reimbursement form's about where bank
    /// details go. Neither belongs on the other's page.
    /// </summary>
    public required string Section1NoticeTitle { get; init; }

    public required string Section1NoticeBody { get; init; }

    public required string Section2Caption { get; init; }

    public required string Section3Caption { get; init; }
    public required string Section3Hint { get; init; }

    /// <summary>
    /// The question asked of every receipt. It was section 2's narrative prompt on the paper forms,
    /// asked once for the whole claim; the accounts team asked for it per receipt.
    /// </summary>
    public required string PurposePrompt { get; init; }

    public required string PurposeHint { get; init; }

    public required string GrossTotalLabel { get; init; }
    public required string LessPersonalLabel { get; init; }
    public required string NetTotalLabel { get; init; }

    public required string Section4Caption { get; init; }

    /// <summary>
    /// The questions this form asks, in the order it asks them - six on the debit card form, five on
    /// the reimbursement form. Only two are word-for-word identical between the forms. A shared string
    /// would be a compliance error, not a cosmetic one.
    /// </summary>
    public required IReadOnlyList<ComplianceQuestion> ComplianceQuestions { get; init; }

    public required string ComplianceDetailsCaption { get; init; }

    /// <summary>
    /// Caption of the attendee table a Yes on question 1 or 2 opens, or null on the form that has no
    /// such table. Only the debit card form has one.
    /// </summary>
    public required string? DetailTableCaption { get; init; }

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
    /// The closing disclaimer under section 6. This one is the app's own words rather than the paper
    /// form's, and the two differ only in whether the debit card policy is named - but it is still
    /// per-kind, so the page has no <c>@if</c> on the kind for a line of prose.
    /// </summary>
    public required string Footnote { get; init; }
}

/// <summary>
/// One section 4 question: which stored answer it is, the text, and the instruction that follows "Yes".
/// </summary>
/// <remarks>
/// Split in two because the page renders them differently - the question beside a No/Yes pair, the
/// instruction inside the panel that a Yes reveals. The paper form prints them as one paragraph.
/// </remarks>
/// <param name="Slot">
/// The stored answer this question reads and writes, 0 to 5 for <c>ComplianceQ1</c> to
/// <c>ComplianceQ6</c>. <b>Not the number on screen</b>, which is the question's position: the
/// reimbursement form no longer asks the paper form's question 1, so its first question is slot 1.
/// </param>
/// <param name="Question">The question as the paper form prints it.</param>
/// <param name="YesInstruction">The instruction that follows "Yes", revealed only once Yes is chosen.</param>
/// <param name="Hint">
/// Plain-English help shown <b>before</b> the question is answered, or null where the question needs
/// none. <b>The app's own words, not the paper form's</b> - which is why it is a separate property
/// rather than folded into <see cref="Question"/>: the question is quoted text and must not be
/// reworded, but nothing stops the page explaining it alongside.
/// <para>
/// Only the Responsible Person and conflict-of-interest questions carry one. They are the two that
/// recite ACNC vocabulary - "Responsible Person", "related entity", "perceived conflict" - at somebody
/// who has just bought morning tea. The Responsible Person hint ends "If unsure, tick No" at the
/// accounts team's request.
/// </para>
/// </param>
public record ComplianceQuestion(int Slot, string Question, string YesInstruction, string? Hint = null);

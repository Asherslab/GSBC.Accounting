namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One submitted form, of either kind. This is the aggregate root: the two paper forms are modelled as
/// one type with a <see cref="SubmissionKind"/> discriminator, not as two parallel aggregates.
/// </summary>
/// <remarks>
/// The two forms share sections 0 to 6, the per-receipt section 3, the compliance questions and the
/// five declarations <b>structurally</b>. They differ in a handful of section 1 fields, grouped below,
/// and in most of the <b>wording</b> of sections 4, 5 and 6.
/// <para>
/// <b>Never share a question or declaration's text between the two kinds.</b> The answers live in
/// shared columns here, but every question and declaration is a per-kind string in the UI and the PDF,
/// because a tick recorded against wording the claimant was never shown is the failure that matters on
/// a compliance document.
/// </para>
/// <para>
/// This is not a ledger entry and must not pretend to be one. The finance destination is Xero; this
/// captures the claim, its evidence and its declarations so a human reviewer can check them.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseSubmission : IIdentifiable
{
    public Guid Id { get; init; }

    /// <summary>Which paper form this is. Decides every per-kind label, question and declaration.</summary>
    public required SubmissionKind Kind { get; init; }

    public required SubmissionStatus Status { get; init; }

    /// <summary>
    /// The short claim reference - <c>RE-2026-0142</c> or <c>DC-2026-0087</c> - issued when the form is
    /// submitted, and null on every draft. Beside <see cref="Id"/> rather than instead of it: the GUID
    /// remains the key everything is addressed by, and this is the one a person can read out.
    /// </summary>
    public string? Reference { get; init; }

    // ---- Section 1, shared ------------------------------------------------------------------------

    /// <summary>`Cardholder name` on the debit card form, `Claimant name` on the reimbursement form.</summary>
    public string? SubmitterName { get; init; }

    /// <summary>`Form date` on the debit card form, `Claim date` on the reimbursement form.</summary>
    public DateTime? FormDate { get; init; }

    // ---- Section 1, debit card only ---------------------------------------------------------------

    /// <summary>
    /// The last four digits, and nothing more. The form prints, verbatim: "Card security: Record only
    /// the last four digits. Never record the full card number, PIN or security code on this form."
    /// That is a constraint on this app too - no contract, column, log line or PDF anywhere holds more
    /// of the card number than this.
    /// </summary>
    public string? CardLastFourDigits { get; init; }

    // ---- Section 1, reimbursement only -------------------------------------------------------------

    public string? ContactPhoneEmail { get; init; }

    /// <summary>`Ministry / department`. Reimbursement form only - the debit card form does not ask.</summary>
    public string? MinistryDepartment { get; init; }

    public PaymentMethod? PaymentMethod { get; init; }

    /// <summary>The free text after `☐ Other:` on the payment method.</summary>
    public string? PaymentMethodOther { get; init; }

    /// <summary>
    /// This is the whole of the banking data model: the app collects no BSB and no account number. A
    /// "No" is handled off-channel - the page tells the claimant to email their details to the accounts
    /// team. Adding an account-number field would be a new decision and a new class of data at rest, not
    /// an implementation detail.
    /// </summary>
    public bool? BankDetailsOnFile { get; init; }

    // ---- Section 2: authorisation, optional on both forms -----------------------------------------------

    /// <summary>`Has this expense been approved?` Null is unanswered, not No.</summary>
    public bool? PriorApprovalGiven { get; init; }

    /// <summary>`Who approved it?` Only meaningful when <see cref="PriorApprovalGiven"/> is true.</summary>
    public string? PriorApprovalBy { get; init; }

    // ---- Section 3 totals ----------------------------------------------------------------------------

    /// <summary>
    /// `Total card transaction` / `Subtotal of receipts`. <b>Server-computed</b> from the lines; a value
    /// arriving from a client is a display convenience that gets overwritten.
    /// </summary>
    public decimal GrossTotal { get; init; }

    /// <summary>Sum of the lines' GST. Server-computed.</summary>
    public decimal GstTotal { get; init; }

    /// <summary>
    /// `Less personal portion to be repaid immediately` / `Less personal / non-reimbursable portion`.
    /// Claimant-entered, not computed - only the claimant knows what part was personal.
    /// </summary>
    public decimal LessPersonalAmount { get; init; }

    /// <summary>
    /// `NET AUTHORISED CHURCH EXPENSE` / `TOTAL REIMBURSEMENT CLAIMED`. Server-computed as
    /// <see cref="GrossTotal"/> minus <see cref="LessPersonalAmount"/>.
    /// </summary>
    public decimal NetTotal { get; init; }

    // ---- Section 4: six compliance answers ------------------------------------------------------------

    /*
       Six bool? columns on the header, not a table. There are exactly six and they are fixed by the
       paper forms; a table would make "was question 4 answered" a join.

       null means NOT ANSWERED, which is a different fact from "No" and the one a reviewer needs to see.
       The form's job is to ask the questions and record the answers so a human sees them - nothing here
       enforces the rules the questions recite.

       The ANSWERS are shared columns. The QUESTIONS are not: only Q4 and Q6 are word-for-word identical
       between the two forms. See ExpenseFormWording.

       ComplianceQ1 is DEBIT CARD ONLY. The reimbursement form's question 1 (motor vehicle travel) was
       removed at the accounts team's request, so on a reimbursement it is always null and the form
       asks five questions, numbered 1 to 5 on screen but stored in Q2 to Q6.
    */
    public bool? ComplianceQ1 { get; init; }
    public bool? ComplianceQ2 { get; init; }
    public bool? ComplianceQ3 { get; init; }
    public bool? ComplianceQ4 { get; init; }
    public bool? ComplianceQ5 { get; init; }
    public bool? ComplianceQ6 { get; init; }

    /// <summary>
    /// Section 4's free-text block. The caption differs per kind, and the debit card version also
    /// collects personal-repayment detail here.
    /// </summary>
    public string? ComplianceDetails { get; init; }

    // ---- Section 6: five declarations -----------------------------------------------------------------

    /*
       Five bool? columns, same reasoning as the compliance answers. Only D4 is word-for-word identical
       between the forms, and D3 is a DIFFERENT declaration on each - repayment on the debit card form,
       no-double-claim on the reimbursement form. Neither form carries the other's.
    */
    public bool? Declaration1 { get; init; }
    public bool? Declaration2 { get; init; }
    public bool? Declaration3 { get; init; }
    public bool? Declaration4 { get; init; }
    public bool? Declaration5 { get; init; }

    // ---- Section 6 signature ---------------------------------------------------------------------------

    /// <summary>
    /// The typed signature. Not a signature in any legal sense - it is a name the claimant typed beside
    /// the five declarations. Approval is the accounts team's, and happens outside this app.
    /// </summary>
    public string? SignatureName { get; init; }

    public DateTime? SignedAt { get; init; }

    // ---- Audit ------------------------------------------------------------------------------------------

    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Last write of any kind, autosaves included. What the drafts list sorts on, and what the
    /// abandoned-draft purge counts ninety days from.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    public DateTime? SubmittedAt { get; init; }

    /// <summary>
    /// Marks mock data, so a demonstration submission is never mistaken for a real claim. Set by the
    /// environment-gated mock-data button and by nothing else.
    /// </summary>
    public bool IsMockData { get; init; }

    /// <summary>Section 3: one entry per receipt, each carrying its own evidence.</summary>
    public List<ExpenseDetail> Details { get; init; } = [];

    /// <summary>Section 4's meals/hospitality table. Debit card form only.</summary>
    public List<ExpenseAttendee> Attendees { get; init; } = [];

    /// <summary>
    /// The uploaded evidence.
    /// </summary>
    /// <remarks>
    /// Carried on the aggregate so a claimant resuming a draft sees the receipts they already attached.
    /// Without it the attachments card would come back empty on a page that had three files against it,
    /// and the obvious next move - attaching them again - is the one that would look like it worked.
    /// </remarks>
    public List<ExpenseAttachment> Attachments { get; init; } = [];

    /// <summary>
    /// Section 5, present only when some detail carries no receipt from the place it was bought.
    /// </summary>
    public MissingReceiptDeclaration? MissingReceipt { get; init; }
}

/// <summary>Which of the two paper forms a submission is.</summary>
[ProtoContract]
public enum SubmissionKind
{
    DebitCardPurchase,
    ExpenseReimbursement
}

/// <summary>
/// Only <see cref="Draft"/> and <see cref="Submitted"/> are reachable in this scope. The rest exist so
/// the approval work that follows is additive rather than a migration of live rows.
/// </summary>
[ProtoContract]
public enum SubmissionStatus
{
    Draft,
    Submitted,
    Approved,
    Declined,
    Paid
}

/// <summary>`☐ EFT   ☐ Other:`, reimbursement form only.</summary>
[ProtoContract]
public enum PaymentMethod
{
    Eft,
    Other
}

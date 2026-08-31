namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One submitted form, of either kind. This is the aggregate root: the two paper forms are modelled as
/// one type with a <see cref="SubmissionKind"/> discriminator, not as two parallel aggregates.
/// </summary>
/// <remarks>
/// The two forms share all eight sections, the seven-column line table, the six compliance questions
/// and the five declarations <b>structurally</b>. They differ in 19 header fields, which are the
/// nullable properties grouped below, and they differ in almost all of their <b>wording</b>.
/// <para>
/// <b>Never share the printed text between the two kinds.</b> Only two of the six compliance questions
/// and one of the five declarations are word-for-word identical; sections 2, 3 and 5 differ too. The
/// answers live in shared columns here, but every question, declaration and label is a per-kind string
/// in the UI and the PDF. On a compliance document, a form that does not say what the paper form says
/// is the failure that matters.
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

    // ---- Section 1, shared ------------------------------------------------------------------------

    /// <summary>`Cardholder name` on the debit card form, `Claimant name` on the reimbursement form.</summary>
    public string? SubmitterName { get; init; }

    /// <summary>`Form date` on the debit card form, `Claim date` on the reimbursement form.</summary>
    public DateTime? FormDate { get; init; }

    public ClaimantRole? Role { get; init; }

    /// <summary>The free text after `☐ Other:` on the role checkboxes.</summary>
    public string? RoleOther { get; init; }

    /// <summary>Section 1 `Ministry / department`, printed on both forms.</summary>
    public string? MinistryDepartment { get; init; }

    // ---- Section 1, debit card only ---------------------------------------------------------------

    /// <summary>
    /// The last four digits, and nothing more. The form prints, verbatim: "Card security: Record only
    /// the last four digits. Never record the full card number, PIN or security code on this form."
    /// That is a constraint on this app too - no contract, column, log line or PDF anywhere holds more
    /// of the card number than this.
    /// </summary>
    public string? CardLastFourDigits { get; init; }

    public DateTime? TransactionDate { get; init; }

    /// <summary>Printed as `Time: ________` - free text on the paper form, not a picker.</summary>
    public string? TransactionTime { get; init; }

    public string? SupplierMerchant { get; init; }

    /// <summary>
    /// What the card was actually charged, as stated by the claimant. Distinct from
    /// <see cref="GrossTotal"/>, which is the sum of the lines - reconciling the two is the point of
    /// the debit card form. The reimbursement form has no equivalent, because nothing external states
    /// its total.
    /// </summary>
    public decimal? AmountCharged { get; init; }

    /// <summary>Printed as `Ref: __________________` beside the amount charged.</summary>
    public string? BankReference { get; init; }

    // ---- Section 1, reimbursement only -------------------------------------------------------------

    public string? ContactPhoneEmail { get; init; }

    public DateTime? ExpensePeriodFrom { get; init; }

    public DateTime? ExpensePeriodTo { get; init; }

    public PaymentMethod? PaymentMethod { get; init; }

    /// <summary>The free text after `☐ Other:` on the payment method.</summary>
    public string? PaymentMethodOther { get; init; }

    /// <summary>
    /// `☐ Yes   ☐ No - provide securely`. This is the whole of the banking data model: the form
    /// deliberately collects no BSB and no account number, and neither does this app. A "No" is handled
    /// off-channel, per the form's own instruction ("Do not email bank details in an unsecured message.
    /// Use the church-approved secure method."). Adding an account-number field would be a new decision
    /// and a new class of data at rest, not an implementation detail.
    /// </summary>
    public bool? BankDetailsOnFile { get; init; }

    // ---- Section 2 ----------------------------------------------------------------------------------

    /// <summary>
    /// Section 2 slot 1: `Ministry / department` on the debit card form (which repeats section 1's
    /// answer), `Purpose / activity` on the reimbursement form.
    /// </summary>
    public string? PurposeActivity { get; init; }

    public string? EventProject { get; init; }

    public string? PriorApprovalBy { get; init; }

    public DateTime? ApprovalDate { get; init; }

    /// <summary>
    /// The written answer to section 2's narrative prompt. The prompt itself differs per kind and is
    /// three questions in one box on the debit card form.
    /// </summary>
    public string? PurposeNarrative { get; init; }

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
       between the two forms, and Q1 is a materially different question. See ExpenseFormWording.
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
    /// the five declarations, and section 7's approval is what carries authority.
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

    public List<ExpenseLine> Lines { get; init; } = [];

    /// <summary>Section 4's meals/hospitality table. Debit card form only.</summary>
    public List<ExpenseAttendee> Attendees { get; init; } = [];

    /// <summary>Section 4's motor vehicle trip record. Reimbursement form only.</summary>
    public List<ExpenseTrip> Trips { get; init; } = [];

    /// <summary>
    /// The uploaded evidence.
    /// </summary>
    /// <remarks>
    /// Carried on the aggregate so a claimant resuming a draft sees the receipts they already attached.
    /// Without it the attachments card would come back empty on a page that had three files against it,
    /// and the obvious next move - attaching them again - is the one that would look like it worked.
    /// </remarks>
    public List<ExpenseAttachment> Attachments { get; init; } = [];

    /// <summary>Section 5, present only when some line is marked Missing.</summary>
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

/// <summary>
/// `Role / relationship`, six checkboxes, printed identically on both forms. Nullable rather than
/// carrying an `Unknown` member: an `Unknown` reads as a value, and a value is something that ends up
/// written down as though a human had chosen it.
/// </summary>
[ProtoContract]
public enum ClaimantRole
{
    Employee,
    Volunteer,
    Pastor,
    ResponsiblePerson,
    Other
}

/// <summary>`☐ EFT   ☐ Other:`, reimbursement form only.</summary>
[ProtoContract]
public enum PaymentMethod
{
    Eft,
    Other
}

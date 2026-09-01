using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

/// <summary>
/// Creates a submission in <see cref="SubmissionStatus.Draft"/> and returns its id.
/// </summary>
/// <remarks>
/// <b>Draft is the first half of a two-phase write, and the phases exist because the page is
/// anonymous.</b> The browser posts this to get an id, uploads each receipt against that id, then
/// submits. An attachment endpoint that accepted files with no submission id would be an open write
/// endpoint to the object store.
/// <para>
/// The totals a client sends are display conveniences. The server recomputes <c>GrossTotal</c>,
/// <c>GstTotal</c> and <c>NetTotal</c> from the lines and writes its own numbers - see
/// <c>ExpenseTotals</c>. <see cref="LessPersonalAmount"/> is the exception: only the claimant knows
/// what part of a purchase was personal, so it is taken as given here and reconciled at submit.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record CreateExpenseSubmissionRequest
{
    public required SubmissionKind Kind { get; init; }

    // Section 1, shared
    public string? SubmitterName { get; init; }
    public DateTime? FormDate { get; init; }
    public ClaimantRole? Role { get; init; }
    public string? RoleOther { get; init; }
    public string? MinistryDepartment { get; init; }

    // Section 1, debit card only
    public string? CardLastFourDigits { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? TransactionTime { get; init; }
    public string? SupplierMerchant { get; init; }
    public decimal? AmountCharged { get; init; }
    public string? BankReference { get; init; }

    // Section 1, reimbursement only
    public string? ContactPhoneEmail { get; init; }
    public DateTime? ExpensePeriodFrom { get; init; }
    public DateTime? ExpensePeriodTo { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public string? PaymentMethodOther { get; init; }
    public bool? BankDetailsOnFile { get; init; }

    // Section 2
    public string? PurposeActivity { get; init; }
    public string? EventProject { get; init; }
    public string? PriorApprovalBy { get; init; }
    public DateTime? ApprovalDate { get; init; }
    public string? PurposeNarrative { get; init; }

    // Section 3. There is no LessPersonalAmount here any more: it is the sum of the details' own
    // non-reimbursed amounts, so the server adds it up rather than being told it twice.
    public List<ExpenseDetail> Details { get; init; } = [];

    // Section 4
    public bool? ComplianceQ1 { get; init; }
    public bool? ComplianceQ2 { get; init; }
    public bool? ComplianceQ3 { get; init; }
    public bool? ComplianceQ4 { get; init; }
    public bool? ComplianceQ5 { get; init; }
    public bool? ComplianceQ6 { get; init; }
    public string? ComplianceDetails { get; init; }
    public List<ExpenseAttendee> Attendees { get; init; } = [];
    public List<ExpenseTrip> Trips { get; init; } = [];

    // Section 5, null unless some detail carries no receipt from where the purchase was made
    public MissingReceiptDeclaration? MissingReceipt { get; init; }

    // Section 6
    public bool? Declaration1 { get; init; }
    public bool? Declaration2 { get; init; }
    public bool? Declaration3 { get; init; }
    public bool? Declaration4 { get; init; }
    public bool? Declaration5 { get; init; }

    public string? SignatureName { get; init; }

    /// <summary>
    /// Set only by the environment-gated mock-data button, so a demonstration submission is never
    /// mistaken for a real claim in the database.
    /// </summary>
    public bool IsMockData { get; init; }
}

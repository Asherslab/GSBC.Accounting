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

    // Section 3
    public decimal LessPersonalAmount { get; init; }
    public List<ExpenseLine> Lines { get; init; } = [];

    // Section 6
    public string? SignatureName { get; init; }

    /// <summary>
    /// Set only by the environment-gated mock-data button, so a demonstration submission is never
    /// mistaken for a real claim in the database.
    /// </summary>
    public bool IsMockData { get; init; }
}

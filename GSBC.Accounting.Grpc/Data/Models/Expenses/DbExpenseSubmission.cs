using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>
/// The persisted aggregate root. Column configuration - decimal precision, enum-as-string, the
/// soft-delete filter - is in <c>AccountingDbContext.ExpensesModel.cs</c>.
/// </summary>
/// <remarks>
/// Dates are <c>DateTimeOffset</c> here and UTC <c>DateTime</c> on the contract; the converter bridges
/// them. <b>A <c>DateTimeOffset</c> compared against in a query must have offset zero</b> or Npgsql
/// throws at execution, not at compile time: "Cannot write DateTimeOffset with Offset=10:00:00 to
/// PostgreSQL type 'timestamp with time zone', only offset 0 (UTC) is supported."
/// </remarks>
public class DbExpenseSubmission
{
    public required Guid Id { get; set; }

    public required SubmissionKind Kind { get; set; }

    public required SubmissionStatus Status { get; set; }

    // ---- Section 1, shared ----
    public string? SubmitterName { get; set; }
    public DateTimeOffset? FormDate { get; set; }
    public ClaimantRole? Role { get; set; }
    public string? RoleOther { get; set; }
    public string? MinistryDepartment { get; set; }

    // ---- Section 1, debit card only ----

    /// <summary>
    /// Four digits, and never more. The form prints "Never record the full card number, PIN or security
    /// code on this form", and that is a constraint on this column too.
    /// </summary>
    public string? CardLastFourDigits { get; set; }

    public DateTimeOffset? TransactionDate { get; set; }
    public string? TransactionTime { get; set; }
    public string? SupplierMerchant { get; set; }
    public decimal? AmountCharged { get; set; }
    public string? BankReference { get; set; }

    // ---- Section 1, reimbursement only ----
    public string? ContactPhoneEmail { get; set; }
    public DateTimeOffset? ExpensePeriodFrom { get; set; }
    public DateTimeOffset? ExpensePeriodTo { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentMethodOther { get; set; }

    /// <summary>
    /// The entire banking data model. No BSB, no account number - the paper form collects neither, and
    /// adding one would be a new class of data at rest rather than an implementation detail.
    /// </summary>
    public bool? BankDetailsOnFile { get; set; }

    // ---- Section 2 ----
    public string? PurposeActivity { get; set; }
    public string? EventProject { get; set; }
    public string? PriorApprovalBy { get; set; }
    public DateTimeOffset? ApprovalDate { get; set; }
    public string? PurposeNarrative { get; set; }

    // ---- Section 3 totals, all server-computed except LessPersonalAmount ----
    public decimal GrossTotal { get; set; }
    public decimal GstTotal { get; set; }
    public decimal LessPersonalAmount { get; set; }
    public decimal NetTotal { get; set; }

    // ---- Section 4: six compliance answers, six columns, not a table ----
    // null means NOT ANSWERED, which is a different fact from "No" and the one a reviewer needs to see.
    public bool? ComplianceQ1 { get; set; }
    public bool? ComplianceQ2 { get; set; }
    public bool? ComplianceQ3 { get; set; }
    public bool? ComplianceQ4 { get; set; }
    public bool? ComplianceQ5 { get; set; }
    public bool? ComplianceQ6 { get; set; }

    public string? ComplianceDetails { get; set; }

    // ---- Section 6: five declarations, same reasoning ----
    public bool? Declaration1 { get; set; }
    public bool? Declaration2 { get; set; }
    public bool? Declaration3 { get; set; }
    public bool? Declaration4 { get; set; }
    public bool? Declaration5 { get; set; }

    // ---- Section 6 signature ----
    public string? SignatureName { get; set; }
    public DateTimeOffset? SignedAt { get; set; }

    // ---- Audit ----
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }

    public bool IsMockData { get; set; }

    /// <summary>
    /// Soft delete. ACNC retention is seven years, so nothing here is ever removed. A global query
    /// filter in <c>BuildExpensesModel</c> applies this to every read, including counts;
    /// <c>IgnoreQueryFilters()</c> is the only way past it.
    /// </summary>
    [MapperIgnore]
    public bool Deleted { get; set; }

    /// <summary>
    /// Deliberately NOT [MapperIgnore]d: the lines are part of the aggregate and the contract carries
    /// them. The attribute belongs on back-references and on anything that would walk out of the
    /// aggregate - see DbExpenseLine.Submission - because without it the mapper follows the graph and
    /// either serialises half the database or fails on a cycle.
    /// </summary>
    public List<DbExpenseLine> Lines { get; set; } = [];

    public List<DbExpenseAttachment> Attachments { get; set; } = [];

    public List<DbExpenseAttendee> Attendees { get; set; } = [];

    public List<DbExpenseTrip> Trips { get; set; } = [];

    /// <summary>Section 5. At most one, enforced by a unique index on SubmissionId.</summary>
    public DbMissingReceiptDeclaration? MissingReceipt { get; set; }
}

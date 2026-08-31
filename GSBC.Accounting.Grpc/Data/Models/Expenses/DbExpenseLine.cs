using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>One row of section 3's table. Ordered by <see cref="Ordinal"/>, because the paper form is.</summary>
public class DbExpenseLine
{
    public required Guid Id { get; set; }

    public required Guid SubmissionId { get; set; }

    /// <summary>
    /// The back-reference, and the reason [MapperIgnore] exists. Without it the mapper walks from a
    /// line back to its submission and round again.
    /// </summary>
    [MapperIgnore]
    public DbExpenseSubmission? Submission { get; set; }

    public required int Ordinal { get; set; }

    /// <summary>Column 1 on the debit card form (`Item`). Null on a reimbursement line.</summary>
    public string? ItemDescription { get; set; }

    /// <summary>Column 1 on the reimbursement form (`Date`). Null on a debit card line.</summary>
    public DateTimeOffset? LineDate { get; set; }

    public string? Details { get; set; }
    public string? Purpose { get; set; }

    public required EvidenceStatus Evidence { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal? GstAmount { get; set; }

    /// <summary>Pre-printed as 100 on the paper form, so 100 is the default rather than a choice.</summary>
    public decimal ChurchUsePercent { get; set; } = 100m;

    /// <summary>Soft delete, same reasoning as the submission's.</summary>
    [MapperIgnore]
    public bool Deleted { get; set; }
}

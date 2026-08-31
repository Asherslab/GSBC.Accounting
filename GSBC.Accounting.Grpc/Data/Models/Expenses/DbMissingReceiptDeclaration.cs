using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>Section 5, at most one per submission, present only when a line is marked Missing.</summary>
public class DbMissingReceiptDeclaration
{
    public required Guid Id { get; set; }
    public required Guid SubmissionId { get; set; }

    [MapperIgnore]
    public DbExpenseSubmission? Submission { get; set; }

    public string? Supplier { get; set; }
    public DateTimeOffset? Date { get; set; }
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }

    /// <summary>Whether the claimant ticked the declaration paragraph itself.</summary>
    public bool Declared { get; set; }

    [MapperIgnore]
    public bool Deleted { get; set; }
}

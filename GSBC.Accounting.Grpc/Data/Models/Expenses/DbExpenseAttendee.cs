using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>Section 4's meals/hospitality table. Debit card form only.</summary>
public class DbExpenseAttendee
{
    public required Guid Id { get; set; }
    public required Guid SubmissionId { get; set; }

    [MapperIgnore]
    public DbExpenseSubmission? Submission { get; set; }

    public required int Ordinal { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string? Person { get; set; }
    public string? Relationship { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PrivateShare { get; set; }
    public string? Reason { get; set; }

    [MapperIgnore]
    public bool Deleted { get; set; }
}

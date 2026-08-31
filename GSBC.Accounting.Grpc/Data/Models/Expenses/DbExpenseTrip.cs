using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>Section 4's motor vehicle trip record. Reimbursement form only.</summary>
public class DbExpenseTrip
{
    public required Guid Id { get; set; }
    public required Guid SubmissionId { get; set; }

    [MapperIgnore]
    public DbExpenseSubmission? Submission { get; set; }

    public required int Ordinal { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public decimal? BusinessKm { get; set; }

    /// <summary>
    /// Recorded, not applied. The form asks what rate the claimant used; this app holds no ATO rate
    /// table and checks it against nothing. A reviewer does that.
    /// </summary>
    public decimal? ApprovedRate { get; set; }

    public string? Purpose { get; set; }

    [MapperIgnore]
    public bool Deleted { get; set; }
}

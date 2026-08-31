namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// Section 5, present only when some line is marked <see cref="EvidenceStatus.Missing"/>.
/// </summary>
/// <remarks>
/// The three fields and the reason prompt are byte-for-byte identical on both forms. The declaration
/// paragraph itself is not - see <c>ExpenseFormWording.MissingReceiptDeclaration</c>; only its closing
/// GST sentence is shared.
/// <para>
/// A separate entity rather than four more nullable columns, because it is a distinct statement a
/// person made, and a reviewer needs to see it as one thing.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record MissingReceiptDeclaration : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid SubmissionId { get; init; }

    /// <summary>`Supplier:`</summary>
    public string? Supplier { get; init; }

    /// <summary>`Date:`</summary>
    public DateTime? Date { get; init; }

    /// <summary>`Amount:`</summary>
    public decimal? Amount { get; init; }

    /// <summary>`Reason evidence cannot be supplied and steps taken to obtain a copy:`</summary>
    public string? Reason { get; init; }

    /// <summary>Whether the claimant ticked the declaration paragraph itself.</summary>
    public bool Declared { get; init; }
}

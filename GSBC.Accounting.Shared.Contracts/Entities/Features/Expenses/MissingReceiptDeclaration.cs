namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// Section 5, present only when some detail carries no receipt from the place the purchase was made -
/// that is, none of its files is an <see cref="AttachmentKind.SupplierReceipt"/>.
/// </summary>
/// <remarks>
/// A bank line or a banking-app screenshot proves the money moved and who it went to, and says nothing
/// about what it bought. That is the gap this declaration covers, and it is why the trigger is the
/// <b>kind of evidence attached</b> rather than a checkbox somebody ticks: the claimant has already said
/// what each file is, and asking twice is how two answers end up disagreeing.
/// <para>
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

namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One uploaded file: a receipt, a tax invoice, or the supporting evidence behind a line.
/// </summary>
/// <remarks>
/// The metadata here is the point, and it is more than GSBC.ImpactKids' photo store keeps. A receipt is
/// evidence under a seven-year retention obligation, so the original filename, the declared content
/// type, the byte size and the content hash all have to survive: an auditor in year six asking "is this
/// the file that was uploaded" needs an answer that does not depend on the object store still being
/// trustworthy.
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseAttachment : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid SubmissionId { get; init; }

    /// <summary>
    /// The line this evidences, when the claimant said which. Null means it belongs to the submission
    /// as a whole - a bank statement covering several lines, say.
    /// </summary>
    public Guid? LineId { get; init; }

    /// <summary>As the claimant's device named it. Kept for the reviewer, never used to build a key.</summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The type the bytes actually are, not merely the type the upload declared - the two are checked
    /// against each other before anything is stored.
    /// </summary>
    public required string ContentType { get; init; }

    public required long ByteSize { get; init; }

    /// <summary>SHA-256 of the content, hex. Also what the object key is built from.</summary>
    public required string ContentHash { get; init; }

    public required AttachmentKind Kind { get; init; }

    public DateTime UploadedAt { get; init; }
}

/// <summary>
/// What the claimant says this file is. Section 3 asks for an itemised receipt or tax invoice
/// specifically, because a card terminal receipt or a bank line does not show what was bought - so the
/// distinction has to be recordable rather than left to the reviewer to guess from a filename.
/// </summary>
[ProtoContract]
public enum AttachmentKind
{
    ItemisedReceipt,
    TaxInvoice,
    BankOrCardStatement,
    QuoteOrOrder,
    Other
}

namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One uploaded file: a receipt, a bank line, or whatever else evidences one purchase.
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
    /// The purchase this file evidences - <see cref="ExpenseDetail.Key"/>, not the detail's row id.
    /// </summary>
    /// <remarks>
    /// <b>Every file belongs to exactly one detail, and a detail exists because a file was attached.</b>
    /// That is the change the web form makes over the paper one: on paper the receipts are stapled to the
    /// back and a reviewer works out which row each belongs to from the amounts, and here it is recorded.
    /// <para>
    /// Nullable only for the moment between the upload landing and the detail being written - and for
    /// rows that predate details. A null here reads as "evidence for this claim, purchase unstated", and
    /// the PDF lists those separately rather than pretending they belong to the first detail.
    /// </para>
    /// <para>
    /// The <b>key</b> rather than the id, because <c>Update</c> soft-deletes and re-adds every detail on
    /// each autosave, so a row id here would come unlinked seconds after the upload.
    /// </para>
    /// </remarks>
    public Guid? DetailKey { get; init; }

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
/// What the claimant says this file is.
/// </summary>
/// <remarks>
/// <b>The distinction that matters is "did this come from the place you bought it".</b> Everything else
/// - a bank app screenshot, a card terminal slip - proves the money moved and says nothing about what it
/// bought, which is the gap section 5's declaration exists to cover. A detail carrying no
/// <see cref="SupplierReceipt"/> is what makes that declaration required.
/// <para>
/// <b>There is no longer a separate "itemised receipt" kind, and that is deliberate.</b> Whether the
/// evidence itemises is now a question the claimant is asked outright, per detail
/// (<see cref="ExpenseDetail.ReceiptIsItemised"/>), rather than something inferred from which entry
/// somebody picked out of a dropdown before choosing the file.
/// </para>
/// </remarks>
[ProtoContract]
public enum AttachmentKind
{
    /// <summary>A receipt or tax invoice from the supplier - the place the purchase was made.</summary>
    SupplierReceipt,

    /// <summary>A bank or card statement line, or a screenshot of one from a banking app.</summary>
    BankOrCardStatement,

    QuoteOrOrder,
    Other
}

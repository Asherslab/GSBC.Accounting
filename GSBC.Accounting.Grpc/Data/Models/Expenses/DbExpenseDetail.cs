using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Data.Models.Expenses;

/// <summary>
/// One purchase, at one place, with its own evidence. Section 3 is a list of these.
/// </summary>
/// <remarks>
/// Column configuration is in <c>AccountingDbContext.ExpensesModel.cs</c>. See
/// <see cref="ExpenseDetail"/> for why this shape replaced the paper form's line table.
/// </remarks>
public class DbExpenseDetail
{
    public required Guid Id { get; set; }

    public required Guid SubmissionId { get; set; }

    /// <summary>
    /// The back-reference, and the reason [MapperIgnore] exists. Without it the mapper walks from a
    /// detail back to its submission and round again.
    /// </summary>
    [MapperIgnore]
    public DbExpenseSubmission? Submission { get; set; }

    /// <summary>
    /// The client's stable handle, and <b>what an attachment points at</b>. Unique per submission.
    /// </summary>
    /// <remarks>
    /// <c>Update</c> soft-deletes every detail and re-adds it, so <see cref="Id"/> changes on every
    /// autosave and an attachment holding one would come unlinked within seconds of being uploaded. This
    /// is sent by the browser and written back unchanged.
    /// </remarks>
    public required Guid Key { get; set; }

    public required int Ordinal { get; set; }

    public string? Supplier { get; set; }

    public DateTimeOffset? PurchaseDate { get; set; }

    public string? Purpose { get; set; }

    /// <summary>Does this receipt include anything that is not a church expense. Null is unanswered.</summary>
    public bool? ContainsPersonalItems { get; set; }

    /// <summary>Does the evidence itself list what was bought. Null is unanswered.</summary>
    public bool? ReceiptIsItemised { get; set; }

    public decimal TotalIncGst { get; set; }

    public decimal? GstAmount { get; set; }

    /// <summary>
    /// What the church is not being asked to bear. Floored at the itemised personal total by
    /// <c>Submit</c>; a claimant may set it higher as a gift.
    /// </summary>
    public decimal NonReimbursedAmount { get; set; }

    /// <summary>
    /// Part of the aggregate, so deliberately not [MapperIgnore]d - the contract carries them.
    /// </summary>
    public List<DbExpenseDetailItem> Items { get; set; } = [];

    /// <summary>Soft delete, same reasoning as the submission's.</summary>
    [MapperIgnore]
    public bool Deleted { get; set; }
}

/// <summary>One line a claimant typed out of a receipt. Ordered, because the receipt is.</summary>
public class DbExpenseDetailItem
{
    public required Guid Id { get; set; }

    public required Guid DetailId { get; set; }

    [MapperIgnore]
    public DbExpenseDetail? Detail { get; set; }

    public required int Ordinal { get; set; }

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// Church use or not. A yes/no rather than the paper form's percentage - a single item is the
    /// church's or it is not, and a percentage on one is a number somebody invents.
    /// </summary>
    public bool IsChurchUse { get; set; }

    [MapperIgnore]
    public bool Deleted { get; set; }
}

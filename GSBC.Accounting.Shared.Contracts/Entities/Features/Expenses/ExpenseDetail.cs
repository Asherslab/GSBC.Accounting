namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One purchase, at one place, evidenced by one or more attached files.
/// </summary>
/// <remarks>
/// <b>This replaced the paper form's section 3 line table, and the reason is the medium.</b> On paper,
/// section 3 is a grid somebody writes across and the receipts are stapled to the back; nothing on the
/// page says which row a given receipt belongs to, and a reviewer works it out from the amounts. A web
/// form does not have to be that. A detail is <b>created by attaching a receipt</b> and carries its own
/// evidence, so the link is recorded rather than inferred.
/// <para>
/// <b>One receipt, one purchase location, one detail.</b> Several files may hang off one detail - a long
/// receipt photographed in three parts, or a receipt plus the bank line that proves it was paid - but
/// two separate purchases are two details, because the questions below are asked of a single receipt and
/// have no answer for two.
/// </para>
/// <para>
/// The two questions decide how much itemisation is required, and that is the whole point of them: see
/// <see cref="Itemisation"/>. They are <c>bool?</c> for the same reason section 4's answers are - null is
/// unanswered, which is a different fact from "No".
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseDetail : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid SubmissionId { get; init; }

    /// <summary>
    /// The claimant's own stable handle for this detail, minted by the browser and never reassigned.
    /// <b>This, not <see cref="Id"/>, is what an attachment points at.</b>
    /// </summary>
    /// <remarks>
    /// <c>Update</c> replaces a draft's children rather than merging them - a deleted detail has to
    /// disappear, and matching rows up by position across an edit that inserted one in the middle is a
    /// way to silently move somebody's money between purchases. So every autosave gives every detail a
    /// new <see cref="Id"/>, and an attachment holding one would come unlinked two seconds after it was
    /// uploaded. The key survives the rewrite because the client sends the same one back.
    /// </remarks>
    public required Guid Key { get; init; }

    /// <summary>Position on the form, zero-based. The order is the order the claimant attached them.</summary>
    public required int Ordinal { get; init; }

    /// <summary>Where it was bought. One purchase location per detail.</summary>
    public string? Supplier { get; init; }

    public DateTime? PurchaseDate { get; init; }

    /// <summary>What this purchase was for - the church purpose, in the claimant's words.</summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Question one: does this receipt include anything that is not a church expense - personal
    /// shopping picked up in the same transaction, a family member's meal, and so on.
    /// </summary>
    public bool? ContainsPersonalItems { get; init; }

    /// <summary>
    /// Question two: does the evidence itself list what was bought, line by line. A supermarket docket
    /// does; a card terminal slip, a handwritten total or a bank statement line does not.
    /// </summary>
    public bool? ReceiptIsItemised { get; init; }

    /// <summary>The whole receipt, GST included - what the supplier was actually paid.</summary>
    public decimal TotalIncGst { get; init; }

    /// <summary>GST as the evidence states it. Null where the supplier is not registered and shows none.</summary>
    public decimal? GstAmount { get; init; }

    /// <summary>
    /// The part of <see cref="TotalIncGst"/> the church is <b>not</b> being asked to bear.
    /// </summary>
    /// <remarks>
    /// Never below <see cref="PersonalItemsTotal"/>: whatever the claimant itemised as personal is not
    /// claimable, and the field is floored at that. It may be set <b>higher</b> - somebody choosing to
    /// carry part of a legitimate church cost themselves is making a gift, and the form has no business
    /// refusing one.
    /// </remarks>
    public decimal NonReimbursedAmount { get; init; }

    /// <summary>
    /// What the claimant itemised. Empty when <see cref="Itemisation"/> is
    /// <see cref="ItemisationRequirement.None"/>.
    /// </summary>
    public List<ExpenseDetailItem> Items { get; init; } = [];

    /// <summary>
    /// How much itemising this detail needs, derived from the two questions. <b>Never stored</b> - it is
    /// the answers that are a fact about the claim, and a stored derivation is one that can go stale.
    /// </summary>
    /// <remarks>
    /// <b>[ProtoIgnore], and it is not optional.</b> This record is
    /// <c>ImplicitFields = ImplicitFields.AllPublic</c>, which takes every public property including the
    /// computed ones - and then deserialisation tries to <i>assign</i> a get-only property and throws
    /// <c>InvalidOperationException: Cannot apply changes to property ...Itemisation</c>. That surfaces
    /// as "Could not reach the server: Error starting gRPC call" on the form, which points at the
    /// network and not at the contract, so it costs more to diagnose than it should.
    /// <para>
    /// A derived value has no business on the wire in any case: both ends compute it from the two
    /// answers, and sending it would create a second copy for the first to disagree with.
    /// </para>
    /// </remarks>
    [ProtoIgnore]
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>Nothing personal on an itemised receipt</b> - the evidence already lists everything and
    /// all of it is the church's, so a total and the GST are the whole story.</item>
    /// <item><b>Personal items on an itemised receipt</b> - only the personal lines need typing out. The
    /// church's side is already legible on the attached evidence, and re-typing it would be transcription
    /// for its own sake.</item>
    /// <item><b>An unitemised receipt</b> - the evidence says only what was paid, so everything has to be
    /// listed here, church and personal alike. That is the case whether or not anything personal is on
    /// it: with no itemisation there is otherwise no record at all of what the money bought.</item>
    /// </list>
    /// </remarks>
    public ItemisationRequirement Itemisation =>
        (ContainsPersonalItems, ReceiptIsItemised) switch
        {
            (false, true) => ItemisationRequirement.None,
            (_, false) => ItemisationRequirement.Everything,
            (true, true) => ItemisationRequirement.PersonalItemsOnly,
            // Not answered yet. Nothing is required of a question nobody has answered, and the page
            // shows no item table until both are in.
            _ => ItemisationRequirement.None
        };

    /// <summary>
    /// The itemised lines that are not the church's. <b>The floor under
    /// <see cref="NonReimbursedAmount"/>.</b>
    /// </summary>
    /// <remarks>[ProtoIgnore] for the same reason as <see cref="Itemisation"/>.</remarks>
    [ProtoIgnore]
    public decimal PersonalItemsTotal =>
        Math.Round(Items.Where(x => !x.IsChurchUse).Sum(x => Math.Round(x.Amount, 2, MidpointRounding.ToEven)),
            2, MidpointRounding.ToEven);
}

/// <summary>
/// One line the claimant typed out of a receipt.
/// </summary>
/// <remarks>
/// This is not the paper form's line - that was a whole receipt on the reimbursement form and a section
/// of one card transaction on the debit card form. This is an <b>item</b>: a thing that was bought, at a
/// price, that is either the church's or it is not.
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseDetailItem : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid DetailId { get; init; }

    public required int Ordinal { get; init; }

    /// <summary>What it was, as it reads on the receipt or as near as the claimant can get.</summary>
    public string? Description { get; init; }

    /// <summary>What it cost, GST included.</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Church use or not, and deliberately a <b>yes or no</b> rather than the paper form's percentage.
    /// </summary>
    /// <remarks>
    /// The percentage column made sense on a form where a line was a whole receipt - "this $80 shop was
    /// 60% church" is a real thing to say about a basket. It makes no sense about a single item: a packet
    /// of paper plates is the church's or it is yours, and a claimant asked to put a percentage on one
    /// invents a number. Where a purchase genuinely splits, it splits into items, which is what the
    /// itemisation is for.
    /// </remarks>
    public bool IsChurchUse { get; init; }
}

/// <summary>How much of a receipt has to be typed out. Derived from the detail's two questions.</summary>
[ProtoContract]
public enum ItemisationRequirement
{
    /// <summary>Nothing personal, and the evidence itemises itself. A total and the GST will do.</summary>
    None,

    /// <summary>An itemised receipt with personal items on it. Type out the personal ones only.</summary>
    PersonalItemsOnly,

    /// <summary>Evidence that does not itemise. Type out everything, church and personal alike.</summary>
    Everything
}

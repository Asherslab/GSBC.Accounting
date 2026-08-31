namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One row of the debit card form's `Meals / hospitality / gifts / travel details` table, opened by a
/// Yes on compliance question 1 or 2.
/// </summary>
/// <remarks>
/// Six columns, not the four the scope doc summarised. <see cref="PrivateShare"/> is the one that
/// carries weight: the form's whole purpose in asking who attended is to separate the church's share
/// from a private one, and a table that recorded attendees without amounts would answer the question
/// without answering the point of it.
/// <para>Three blank rows on the paper form is page space, not a limit.</para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseAttendee : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid SubmissionId { get; init; }

    public required int Ordinal { get; init; }

    /// <summary>Column 1: `Date`.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Column 2: `Person / recipient`.</summary>
    public string? Person { get; init; }

    /// <summary>Column 3: `Relationship / role`.</summary>
    public string? Relationship { get; init; }

    /// <summary>Column 4: `Amount`.</summary>
    public decimal? Amount { get; init; }

    /// <summary>Column 5: `Private share` - the part that is not a church expense.</summary>
    public decimal? PrivateShare { get; init; }

    /// <summary>Column 6: `Reason and Church purpose`.</summary>
    public string? Reason { get; init; }
}

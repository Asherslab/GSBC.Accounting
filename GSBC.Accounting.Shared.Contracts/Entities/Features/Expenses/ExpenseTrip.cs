namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One row of the reimbursement form's `Motor vehicle trip record`, opened by a Yes on compliance
/// question 1.
/// </summary>
/// <remarks>
/// The debit card form has no equivalent, because its question 1 is a genuinely different question -
/// incidental travel costs paid on the card, wanting a receipt - rather than a rephrasing of this one.
/// This form's question 1 asks about the claimant's own vehicle at a per-kilometre rate and explicitly
/// excludes fuel, so the two open different tables.
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ExpenseTrip : IIdentifiable
{
    public Guid Id { get; init; }

    public required Guid SubmissionId { get; init; }

    public required int Ordinal { get; init; }

    /// <summary>Column 1: `Date`.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Column 2: `From`.</summary>
    public string? From { get; init; }

    /// <summary>Column 3: `To`.</summary>
    public string? To { get; init; }

    /// <summary>Column 4: `Business km`.</summary>
    public decimal? BusinessKm { get; init; }

    /// <summary>
    /// Column 5: `Approved rate`, dollars per kilometre. Recorded rather than applied - the form asks
    /// what rate the claimant used, and this app does not hold an ATO rate table or check it against
    /// one. That is a reviewer's job.
    /// </summary>
    public decimal? ApprovedRate { get; init; }

    /// <summary>Column 6: `Church purpose`.</summary>
    public string? Purpose { get; init; }
}

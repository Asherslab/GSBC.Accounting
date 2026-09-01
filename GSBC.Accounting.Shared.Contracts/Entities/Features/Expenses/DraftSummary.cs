namespace GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

/// <summary>
/// One row of "your saved drafts". Enough to recognise a form, and nothing more.
/// </summary>
/// <remarks>
/// <b>Deliberately not the whole aggregate.</b> The list page renders every draft a session owns, and
/// sending each one's lines, attendees, trips and declarations to draw six fields would move a great
/// deal of a claimant's personal data across the wire for no reader. The full form arrives only when
/// somebody actually opens one, through <c>Get</c>.
/// <para>
/// There is no status field: this only ever describes a <c>Draft</c>. A submitted claim is evidence and
/// leaves the list the moment it is submitted, because it is no longer something anyone may resume.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record DraftSummary
{
    public required Guid Id { get; init; }

    public required SubmissionKind Kind { get; init; }

    /// <summary>
    /// Whoever section 1 names, which on an early draft is usually nobody. The page says "Unnamed
    /// draft" rather than showing a blank - a row with no label reads as a broken list.
    /// </summary>
    public string? SubmitterName { get; init; }

    /// <summary>Section 2's purpose, used as the second line so two drafts of a kind are tellable apart.</summary>
    public string? PurposeActivity { get; init; }

    public decimal GrossTotal { get; init; }

    /// <summary>How many receipts have been entered - one detail per purchase.</summary>
    public int DetailCount { get; init; }

    /// <summary>
    /// Shown because a draft with no receipt cannot be submitted, so the count is the one number that
    /// tells a claimant what is still owed on this form before they open it.
    /// </summary>
    public int AttachmentCount { get; init; }

    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// What the list sorts on, newest first - "where I was" is the question this page answers, and the
    /// last thing edited is nearly always the answer.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// When the abandoned-draft purge will soft-delete this if nobody touches it again. Sent rather
    /// than computed in the browser so the page cannot disagree with the server about a deletion date
    /// it is warning somebody about.
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}

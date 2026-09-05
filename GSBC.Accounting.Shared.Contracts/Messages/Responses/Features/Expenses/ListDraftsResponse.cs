using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.Shared.Contracts.Messages.Responses.Features.Expenses;

/// <summary>
/// The caller's saved drafts, newest first.
/// </summary>
/// <remarks>
/// Its own type rather than <c>BasicReadResponse&lt;List&lt;DraftSummary&gt;&gt;</c>, following the note
/// on <see cref="BasicResponse"/>: response shapes here declare their own fields rather than lean on a
/// base or a generic, because a protobuf-net contract that loses members does so silently.
/// <para>
/// <b>An empty list and a missing session are the same answer.</b> A browser with no cookie is told it
/// has no drafts, not that it has no session - there is nothing useful it could do with the
/// distinction, and a response that admitted to the difference would be a way to ask the server
/// whether a given cookie is live.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ListDraftsResponse
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<DraftSummary> Drafts { get; set; } = [];

    /// <summary>
    /// Claims this browser has already submitted, newest first.
    /// </summary>
    /// <remarks>
    /// <b>Not resumable, and this is not a second drafts list.</b> Once a claim is submitted the only
    /// thing this app offers for it is its reference and its PDF - it is evidence, and the form cannot
    /// edit it. It is here because the claimant's only record was otherwise the browser tab they were
    /// looking at: close it, and they had a reference they did not write down and a drafts list that no
    /// longer contained the claim.
    /// <para>
    /// Answered in the same call as the drafts because it is the same question - "what has this browser
    /// got?" - and a second round trip to say "and one more thing" would be a second chance to fail.
    /// </para>
    /// </remarks>
    public List<DraftSummary> Submitted { get; set; } = [];

    public static ListDraftsResponse Empty() => new() { Success = true };

    public static ListDraftsResponse WithError(string error) => new() { Success = false, Error = error };
}

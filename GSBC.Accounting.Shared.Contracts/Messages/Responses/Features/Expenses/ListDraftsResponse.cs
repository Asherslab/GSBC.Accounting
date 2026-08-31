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

    public static ListDraftsResponse Empty() => new() { Success = true };

    public static ListDraftsResponse WithError(string error) => new() { Success = false, Error = error };
}

namespace GSBC.Accounting.Shared.Contracts.Messages.Responses.Features.Expenses;

/// <summary>
/// The answer to a submit attempt: the claim reference when it succeeded, and everything wrong with
/// the form when it did not.
/// </summary>
/// <remarks>
/// Its own type rather than <c>BasicResponse</c>, and it declares its own fields rather than deriving -
/// see the note on <c>BasicResponse</c> about protobuf-net losing a base type's members silently.
/// <para>
/// It exists for <see cref="Reference"/>. The reference is minted by the server at the moment of
/// submission and the page has no other way to learn it: a submitted claim is deliberately not readable
/// through <c>Read</c> - it is evidence, and the form cannot edit it - so a second round trip to fetch
/// it was not available even in principle.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SubmitExpenseSubmissionResponse
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    /// <summary>
    /// The refusals as plain sentences. The same list as <see cref="Problems"/>, flattened, so that a
    /// caller reading a refusal as text still works.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// The refusals, each knowing where on the page it sends the claimant.
    /// </summary>
    public List<SubmissionProblem> Problems { get; set; } = [];

    /// <summary>
    /// The claim reference, set only on success. <c>RE-2026-0142</c> or <c>DC-2026-0087</c>.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// A refusal that is about the request rather than about the form - no session, no such
    /// submission, already submitted. It has no section to send anybody to, so it carries no problem.
    /// </summary>
    public static SubmitExpenseSubmissionResponse WithError(string error) =>
        new() { Success = false, Error = error, Errors = [error] };

    public static SubmitExpenseSubmissionResponse WithProblems(IEnumerable<SubmissionProblem> problems)
    {
        List<SubmissionProblem> list = problems.ToList();

        return new SubmitExpenseSubmissionResponse
        {
            Success = false,
            Error = list.FirstOrDefault()?.Message,
            Errors = list.Select(x => x.Message).ToList(),
            Problems = list
        };
    }
}

/// <summary>
/// One reason a form was refused, and where on the page to go and fix it.
/// </summary>
/// <remarks>
/// <b>The destination travels with the message rather than being worked out from it.</b> The page could
/// parse "Section 3:" off the front of each sentence, and that would break the first time somebody
/// rewords one - silently, into a list of links that all go to the top of the form. Where the server
/// knows which field is at fault it says so outright, which is also the only way a per-purchase refusal
/// can point at the right one of six identical panels.
/// </remarks>
/// <param name="Message">
/// What to tell the claimant. Leads with its section - see <c>ErrorConstants</c> - because it is read
/// in a list of eight under a form with eight sections.
/// </param>
/// <param name="Anchor">
/// The id of the section card this is about, without the <c>#</c>: <c>s0</c> through <c>s8</c>. Always
/// present, so every item in the list is a link even when nothing narrower can be named.
/// </param>
/// <param name="FieldId">
/// The id of the one field at fault, where the refusal is about exactly one. Null for anything about a
/// section as a whole - six unanswered compliance questions, five unagreed declarations - and for the
/// card reconciliation, which is about two figures in different sections rather than either of them.
/// </param>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record SubmissionProblem(string Message, string Anchor, string? FieldId = null)
{
    /// <summary>
    /// protobuf-net deserialises into a parameterless constructor. Every call site uses the positional
    /// one.
    /// </summary>
    public SubmissionProblem() : this(string.Empty, string.Empty)
    {
    }

    /// <summary>
    /// Where a link to this refusal should point, without the <c>#</c>. Computed, so it is not on the
    /// wire: <c>ImplicitFields.AllPublic</c> would otherwise take it for a field and refuse to build a
    /// serialiser for a property it cannot set.
    /// </summary>
    [ProtoIgnore]
    public string Target => string.IsNullOrEmpty(FieldId) ? Anchor : FieldId;
}

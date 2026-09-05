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

    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// The claim reference, set only on success. <c>RE-2026-0142</c> or <c>DC-2026-0087</c>.
    /// </summary>
    public string? Reference { get; set; }

    public static SubmitExpenseSubmissionResponse WithError(string error) =>
        new() { Success = false, Error = error, Errors = [error] };

    public static SubmitExpenseSubmissionResponse WithErrors(IEnumerable<string> errors)
    {
        List<string> list = errors.ToList();

        return new SubmitExpenseSubmissionResponse
        {
            Success = false,
            Error = list.FirstOrDefault(),
            Errors = list
        };
    }
}

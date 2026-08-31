namespace GSBC.Accounting.Shared.Contracts.Messages.Responses.Base;

/// <summary>
/// The shape every mutating call answers with.
/// </summary>
/// <remarks>
/// Deliberately not derived from. protobuf-net only carries a base type's members into a derived
/// contract when the base declares the subtype with <c>[ProtoInclude]</c>, and a missing declaration
/// is a silent loss of every base member rather than an error - so a response type that needs more
/// than this declares its own fields instead of inheriting.
/// <para>
/// <see cref="Errors"/> is a list rather than one string because validation answers with everything
/// wrong at once: a form that reveals its problems one at a time is the thing people give up on.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BasicResponse
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<string> Errors { get; set; } = [];

    public static BasicResponse WithError(string error) => new() { Success = false, Error = error };

    public static BasicResponse WithErrors(IEnumerable<string> errors)
    {
        List<string> list = errors.ToList();

        return new BasicResponse { Success = false, Error = list.FirstOrDefault(), Errors = list };
    }
}

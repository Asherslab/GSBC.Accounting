namespace GSBC.Accounting.Shared.Contracts.Messages.Responses.Base;

/// <summary>
/// A response carrying one entity. <c>Create</c> answers with <c>BasicReadResponse&lt;Guid?&gt;</c>,
/// whose <see cref="Entity"/> is the new submission's id.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BasicReadResponse<T>
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<string> Errors { get; set; } = [];

    public T? Entity { get; set; }

    public static BasicReadResponse<T> WithError(string error) => new() { Success = false, Error = error };

    public static BasicReadResponse<T> WithErrors(IEnumerable<string> errors)
    {
        List<string> list = errors.ToList();

        return new BasicReadResponse<T> { Success = false, Error = list.FirstOrDefault(), Errors = list };
    }
}

namespace GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

/// <summary>
/// Opens one of the caller's own drafts back into the form.
/// </summary>
/// <remarks>
/// The id alone is not sufficient authority - the draft has to belong to the calling session as well.
/// That is the difference between this and every other read in the app before it: a submission id is
/// no longer a credential.
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ReadExpenseSubmissionRequest
{
    public required Guid SubmissionId { get; init; }
}

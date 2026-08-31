namespace GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

/// <summary>
/// Throws away one of the caller's own drafts.
/// </summary>
/// <remarks>
/// <b>Soft-delete, like everything else here.</b> The row and its children are flagged rather than
/// removed, and the attachment objects stay in the store untouched. "Discard" is what the claimant is
/// promised - the draft leaves their list and stops being resumable - and that is exactly what happens;
/// it is not a promise that bytes are destroyed, and the button does not say it is.
/// <para>
/// Only a <c>Draft</c> can be discarded. A submitted claim is evidence, and a claimant cannot withdraw
/// one by pressing a button - that needs a person, and there is no screen for it in this scope.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record DiscardDraftRequest
{
    public required Guid SubmissionId { get; init; }
}

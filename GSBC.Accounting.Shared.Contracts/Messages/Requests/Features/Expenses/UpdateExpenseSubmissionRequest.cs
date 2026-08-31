namespace GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

/// <summary>
/// Rewrites a draft with what the page currently holds.
/// </summary>
/// <remarks>
/// <b>Why this exists:</b> the two-phase write creates the draft as soon as the first receipt is
/// attached, and everything the claimant types afterwards has to reach the server before submit checks
/// it. Without this the row keeps whatever it was created with, and submit refuses - or worse, accepts -
/// a form that no longer matches what is on screen. Observed on 2026-08-31: correcting the amount
/// charged on the page left the stored row at the old figure and the reconciliation error would not
/// clear.
/// <para>
/// Re-sending the whole form is safe here because attachments are keyed to the submission <i>id</i>,
/// not to its contents, so the evidence link survives an edit.
/// </para>
/// <para>
/// Only a <c>Draft</c> can be updated. A submitted claim is evidence and does not change afterwards.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record UpdateExpenseSubmissionRequest
{
    public required Guid SubmissionId { get; init; }

    /// <summary>The same payload as <see cref="CreateExpenseSubmissionRequest"/>, whole.</summary>
    public required CreateExpenseSubmissionRequest Form { get; init; }
}

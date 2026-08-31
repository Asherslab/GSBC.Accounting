namespace GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

/// <summary>
/// Phase two: turns a <c>Draft</c> into a <c>Submitted</c> claim, if it is complete.
/// </summary>
/// <remarks>
/// Carries only the id. Everything the server checks is already stored - the browser re-sending the
/// form here would let a client submit something different from what it uploaded receipts against.
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record SubmitExpenseSubmissionRequest
{
    public required Guid SubmissionId { get; init; }
}

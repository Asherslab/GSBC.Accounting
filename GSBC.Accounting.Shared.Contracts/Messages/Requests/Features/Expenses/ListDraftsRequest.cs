namespace GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;

/// <summary>
/// Asks for the caller's own drafts. Carries nothing.
/// </summary>
/// <remarks>
/// <b>There is no owner field here, and there must never be one.</b> Whose drafts these are is decided
/// entirely by the <c>__gsbc_anon</c> cookie on the request; a claimant id in the body would be a
/// parameter a caller could change, which is the whole bug this feature exists to close.
/// <para>
/// Empty rather than absent because gRPC methods take a message, and an explicit type is a place to
/// hang this paragraph. A kind filter is not offered: a person has at most a handful of drafts and the
/// list shows both forms together, which is the question they are actually asking.
/// </para>
/// </remarks>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public record ListDraftsRequest;

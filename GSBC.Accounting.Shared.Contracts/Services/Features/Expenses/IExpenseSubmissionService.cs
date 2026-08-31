using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Services.Base;

namespace GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;

/// <summary>
/// The one service both form pages call.
/// </summary>
/// <remarks>
/// <b>The [Service] string is the wire path and must match the YARP route pattern</b>
/// (<c>/gRPC/GSBC.Accounting.{service}/{**catch-all}</c> in GSBC.Accounting.YARP/appsettings.json). A
/// mismatch is not a build error - the call falls through to the WASM catch-all route, comes back as
/// index.html with a 200, and grpc-web reports "Bad gRPC response. Invalid content-type value:
/// text/html".
/// <para>
/// <b>Every method here is scoped to the caller's draft session</b>, resolved from the
/// <c>__gsbc_anon</c> cookie and never from anything in a request body. <see cref="Create"/> mints a
/// session if the browser has none; the rest refuse when it does not own the submission. A submission
/// id on its own stopped being sufficient authority when that cookie arrived - see
/// <c>docs/modules/expenses/drafts.md</c>.
/// </para>
/// <para>
/// There is still no approval queue and no finance screen. What can be read back is a claimant's own
/// unsubmitted work and nothing else: once a claim is submitted it leaves the drafts list, and the
/// people who complete sections 7 and 8 have no screen here. The PDF render stays a plain HTTP
/// endpoint rather than a method here, because a rendered document is not a gRPC message.
/// </para>
/// </remarks>
[Service("gRPC/GSBC.Accounting.ExpenseSubmissions")]
public interface IExpenseSubmissionService
    : ICreateService<CreateExpenseSubmissionRequest>
{
    /// <summary>
    /// Rewrites a draft with what the page currently holds. Needed because the draft is created as soon
    /// as the first receipt is attached, long before the claimant has finished typing.
    /// </summary>
    Task<BasicResponse> Update(UpdateExpenseSubmissionRequest request, CallContext context = default);

    /// <summary>
    /// Checks a draft is complete and marks it submitted. Refuses with every problem at once.
    /// </summary>
    Task<BasicResponse> Submit(SubmitExpenseSubmissionRequest request, CallContext context = default);

    /// <summary>
    /// The caller's own unsubmitted drafts, newest first. Answers with an empty list when the browser
    /// has no session, rather than minting one.
    /// </summary>
    Task<ListDraftsResponse> ListDrafts(ListDraftsRequest request, CallContext context = default);

    /// <summary>
    /// One of the caller's own drafts, whole, so a form page can be filled back in from it. Includes
    /// the attachments, without which a resumed form would show no receipts and invite somebody to
    /// upload them twice.
    /// </summary>
    Task<BasicReadResponse<ExpenseSubmission>> Read(
        ReadExpenseSubmissionRequest request,
        CallContext context = default
    );

    /// <summary>
    /// Soft-deletes one of the caller's own drafts so it leaves their list.
    /// </summary>
    Task<BasicResponse> DiscardDraft(DiscardDraftRequest request, CallContext context = default);
}

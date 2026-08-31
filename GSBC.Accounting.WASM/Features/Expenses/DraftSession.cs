using Grpc.Core;
using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Messages.Responses.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Whether this browser has an anonymous session yet, and the drafts it owns if it has.
/// </summary>
/// <remarks>
/// <b>The client cannot read the cookie.</b> <c>__gsbc_anon</c> is <c>HttpOnly</c> on purpose, so the
/// only way to know whether a session exists is to ask the server and read the refusal. That is what
/// this service does, once per app load, so that every screen that wants to say something about drafts
/// asks the same question and gets the same answer.
/// <para>
/// A browser with no session is <b>not</b> an error. It is somebody who has not started a form yet -
/// the common case on a first visit - and the honest UI for that is to say nothing about drafts at all
/// rather than to show a list that cannot be loaded.
/// </para>
/// </remarks>
public class DraftSession(IExpenseSubmissionService submissions)
{
    private ListDraftsResponse? _drafts;
    private bool _asked;

    /// <summary>
    /// True once the server has answered a drafts call for this browser. False while unknown, and false
    /// for a browser the server refuses - which is the state every "your saved drafts" affordance has to
    /// stay hidden behind.
    /// </summary>
    public bool HasSession { get; private set; }

    /// <summary>
    /// The drafts this browser owns, or <c>null</c> when it has no session. Memoised: the landing page
    /// and the drafts list ask the same question on the same visit, and the second one should not cost a
    /// round trip. Pass <paramref name="force"/> after a discard, when the cached list is known stale.
    /// </summary>
    /// <exception cref="RpcException">
    /// A genuine transport or server fault. Only a refusal is swallowed here - callers still have to
    /// show "could not reach the server" for everything else.
    /// </exception>
    public async Task<ListDraftsResponse?> DraftsAsync(bool force = false)
    {
        if (_asked && !force)
            return _drafts;

        try
        {
            _drafts = await submissions.ListDrafts(new ListDraftsRequest());
            HasSession = true;
        }
        catch (RpcException rpc) when (IsRefusal(rpc))
        {
            _drafts = null;
            HasSession = false;
        }
        finally
        {
            // Set even when a fault propagates: an unreachable server is not a browser without a
            // session, and re-asking on the next render loop would be a request per render.
            _asked = true;
        }

        return _drafts;
    }

    /// <summary>
    /// Whether a failure is the server declining to talk to a browser with no session, rather than a
    /// fault worth showing somebody.
    /// </summary>
    /// <remarks>
    /// The HTTP-shaped cases are not paranoia. Everything except <c>Create</c> is behind the
    /// <c>AnonymousSession</c> policy, and the authorization middleware short-circuits with a bare
    /// <c>401</c>/<c>403</c> before the gRPC layer ever runs - so depending on what the proxy in front
    /// of it does to that response, the client can surface it as <c>Internal</c> or <c>Unknown</c> with
    /// the status code only in the message. Matching just the two clean statuses is how a first visit
    /// ends up reading "could not reach the server".
    /// </remarks>
    public static bool IsRefusal(RpcException rpc) =>
        rpc.StatusCode is StatusCode.Unauthenticated or StatusCode.PermissionDenied
        || (rpc.StatusCode is StatusCode.Internal or StatusCode.Unknown
            && (rpc.Status.Detail.Contains("401") || rpc.Status.Detail.Contains("403")));
}

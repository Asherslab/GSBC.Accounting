using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GSBC.Accounting.Grpc.Features.Sessions;

/// <summary>
/// Turns the <c>__gsbc_anon</c> cookie into an authenticated principal, so ownership can be enforced
/// with <c>[Authorize]</c> and a policy rather than by remembering to call
/// <see cref="AnonymousSessions.CurrentAsync"/> in every method.
/// </summary>
/// <remarks>
/// <b>Why a real authentication scheme rather than an interceptor.</b> The previous shape - each method
/// resolving the session itself - was correct everywhere and structurally unenforced: a new method that
/// forgot the call silently fell back to treating a submission id as authority, which is the exact
/// regression the cookie was introduced to close. A scheme plus a deny-by-default fallback policy makes
/// forgetting it a 401 instead of a hole.
/// <para>
/// <b>This authenticates a browser, not a person.</b> See <see cref="AnonymousSessionDefaults"/> - the
/// identity carries one bespoke claim and no name, no email and no user id. It is
/// <c>IsAuthenticated == true</c> because that is what the authorisation stack means by "presented a
/// credential the server accepted", which the cookie genuinely is. It is not a statement about who
/// anybody is, and the moment something needs that, it needs a different scheme.
/// </para>
/// <para>
/// <b>Never mints.</b> <see cref="AnonymousSessions.CurrentAsync"/> is the only call here, so a browser with
/// no cookie is refused rather than issued one - authentication runs on every request including a
/// crawler's, and minting here would hand a session and a database row to every visitor and quietly
/// undo the "minted on the first draft write, never on a page view" rule. <c>Create</c> is the sole
/// minter and is therefore the sole endpoint that opts out of the policy.
/// </para>
/// <para>
/// <b><see cref="AnonymousSessions"/> is resolved from <c>RequestServices</c>, not injected.</b> It is
/// scoped and memoises the lookup for the request; taking it through the constructor would bind a
/// scoped service into a handler the authentication stack creates per scheme, and the whole point of
/// the memoisation is that the handler and the endpoint share one resolution and one query.
/// </para>
/// </remarks>
public class AnonymousSessionHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AnonymousSessions sessions = Context.RequestServices.GetRequiredService<AnonymousSessions>();

        // NoResult, never Fail. "No cookie" is the ordinary state of a first-time visitor, and Fail
        // would turn it into a logged authentication error on every landing-page hit. It also has to
        // stay NoResult for the endpoints that are deliberately reachable without a session - a
        // submitted claim's PDF - which would otherwise see a failed result on a request that is fine.
        if (await sessions.CurrentAsync(Context.RequestAborted) is not { } sessionId)
            return AuthenticateResult.NoResult();

        ClaimsIdentity identity = new(
            [new Claim(AnonymousSessionDefaults.SessionIdClaim, sessionId.ToString())],
            AnonymousSessionDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(
            new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                AnonymousSessionDefaults.AuthenticationScheme));
    }
}

/// <summary>
/// Reads the session id back off an authenticated principal.
/// </summary>
public static class AnonymousSessionPrincipalExtensions
{
    /// <summary>
    /// The anonymous session id on this principal, or null when it carries none.
    /// </summary>
    /// <remarks>
    /// Available so a handler or a future policy can read the session without a database round trip.
    /// Application code should keep using <see cref="AnonymousSessions.CurrentAsync"/>: it is memoised, it
    /// is what renews the cookie, and it works identically on the endpoints that have no policy.
    /// </remarks>
    public static Guid? AnonymousSessionId(this System.Security.Principal.IPrincipal? principal) =>
        (principal as ClaimsPrincipal)?.FindFirst(AnonymousSessionDefaults.SessionIdClaim) is { } claim
        && Guid.TryParse(claim.Value, out Guid id)
            ? id
            : null;
}

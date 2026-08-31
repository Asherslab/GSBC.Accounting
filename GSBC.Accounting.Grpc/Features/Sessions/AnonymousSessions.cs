using System.Security.Cryptography;
using System.Text;
using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Data.Models.Sessions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Features.Sessions;

/// <summary>
/// Resolves the caller's anonymous session from the <c>__gsbc_anon</c> cookie, and mints one when a
/// draft is first written.
/// </summary>
/// <remarks>
/// <b>This authenticates a browser. It does not identify a person, and nothing may treat it as if it
/// did.</b> The distinction is the whole design and it is easy to lose now that the cookie is a real
/// authentication scheme (<see cref="AnonymousSessionHandler"/>) with a real policy in front of it.
/// What the token proves is "this is the browser that saved that draft", presented as a bearer token
/// anybody holding the cookie can replay. It says nothing about who a person is and it is not a
/// credential anyone chose.
/// <para>
/// So: no approval, no finance step, no audit trail naming somebody and nothing else that needs to know
/// who a person is may be hung off this. Sections 7 and 8 of both paper forms are completed by a human
/// who is not the claimant, and that stays true. When real sign-in arrives it gets its own scheme and
/// its own policy beside this one rather than extending this.
/// </para>
/// <para>
/// <b>Minted by this service, carried by the proxy.</b> YARP holds no signing key and no database
/// connection, so it can neither mint this nor check it; it forwards the <c>Cookie</c> header in and
/// the <c>Set-Cookie</c> header back out, and the browser stores the result against the proxy's origin
/// because everything - the WASM app, <c>/gRPC/</c> and <c>/api/</c> - is served from there. This is
/// the shape GSBC.ImpactKids' display auth uses for the same reason: the proxy carries a sealed
/// envelope it cannot open.
/// </para>
/// <para>
/// <b>Minted on the first draft write, never on a page view.</b> <see cref="EnsureAsync"/> is called by
/// <c>Create</c> and by nothing else; everything else uses <see cref="CurrentAsync"/>, which returns
/// null rather than issuing anything. A crawler that loads the landing page therefore leaves no row and
/// no cookie behind, and the cookie stays defensible as strictly necessary to a service the claimant
/// actually asked for - which is what keeps it out of consent-banner territory.
/// </para>
/// </remarks>
public class AnonymousSessions(
    AccountingDbContext db,
    IHttpContextAccessor accessor,
    ILogger<AnonymousSessions> logger
)
{
    /// <summary>
    /// Resolved once per request. Ownership is checked by several methods on one call - update, then
    /// submit - and each would otherwise repeat the lookup.
    /// </summary>
    private bool _resolved;

    private Guid? _sessionId;

    /// <summary>
    /// The caller's session, or null when there is no cookie, the cookie names nothing, or the session
    /// has expired. <b>Never mints one.</b>
    /// </summary>
    public async Task<Guid?> CurrentAsync(CancellationToken token = default)
    {
        if (_resolved)
            return _sessionId;

        _resolved = true;

        HttpContext? http = accessor.HttpContext;

        if (http is null)
            return null;

        string? presented = http.Request.Cookies[AnonymousSessionOptions.CookieName];

        if (string.IsNullOrWhiteSpace(presented))
            return null;

        string hash = Hash(presented);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        DbAnonymousSession? session = await db.AnonymousSessions
            .FirstOrDefaultAsync(x => x.TokenHash == hash, token);

        // An expired session is left in place rather than deleted. It is the only record that a draft
        // ever had an owner, and the purge is what removes both together.
        if (session is null || session.ExpiresAt <= now)
            return null;

        // SLIDING RENEWAL, and the halfway point rather than every request. Somebody who fills in one
        // form a quarter would otherwise walk into a hard expiry they never saw coming; renewing on
        // every request would write to Postgres on every keystroke of an autosave. Past halfway, the
        // next thing they do buys them another full year.
        if (session.ExpiresAt - now < AnonymousSessionOptions.Lifetime - AnonymousSessionOptions.RenewAfter)
        {
            session.LastSeenAt = now;
            session.ExpiresAt = now + AnonymousSessionOptions.Lifetime;

            db.Entry(session).Property(x => x.LastSeenAt).IsModified = true;
            db.Entry(session).Property(x => x.ExpiresAt).IsModified = true;

            await db.SaveChangesAsync(token);

            // Re-sent so the browser's own copy gets the new Max-Age too. Without this the row would
            // outlive the cookie and the claimant would lose their drafts on a schedule the server
            // believed it had already extended.
            Write(http, presented, session.ExpiresAt);
        }

        _sessionId = session.Id;

        return _sessionId;
    }

    /// <summary>
    /// The caller's session, minting and setting the cookie if there is not one.
    /// </summary>
    /// <remarks>
    /// <b>Only <c>Create</c> may call this</b>, because creating a draft is the first moment a browser
    /// has anything worth remembering. Calling it from a read would hand a session to every visitor.
    /// </remarks>
    public async Task<Guid> EnsureAsync(CancellationToken token = default)
    {
        if (await CurrentAsync(token) is { } existing)
            return existing;

        HttpContext? http = accessor.HttpContext
                            ?? throw new InvalidOperationException(
                                "A draft session can only be minted while serving a request.");

        // 256 bits from the OS. The token is the whole of the credential, so it is generated the same
        // way a session cookie anywhere else is, and never from a Guid - Guid.NewGuid is unpredictable
        // in practice but is not documented as cryptographically random, and this is not the place to
        // rely on an implementation detail.
        byte[] entropy = RandomNumberGenerator.GetBytes(32);
        string presented = Base64UrlEncode(entropy);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        DbAnonymousSession session = new()
        {
            // Guid.Empty, not Guid.NewGuid(): Postgres generates the real one, as everywhere else here.
            Id = Guid.Empty,
            TokenHash = Hash(presented),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now + AnonymousSessionOptions.Lifetime,
            UserId = null
        };

        await db.AnonymousSessions.AddAsync(session, token);
        await db.SaveChangesAsync(token);

        Write(http, presented, session.ExpiresAt);

        _resolved = true;
        _sessionId = session.Id;

        logger.LogInformation("Minted draft session {SessionId}", session.Id);

        return session.Id;
    }

    /// <summary>
    /// Sets the cookie. Every attribute here is load-bearing.
    /// </summary>
    /// <remarks>
    /// <b><c>SameSite=Lax</c> is a security control, not a default.</b> Once this cookie authorises
    /// writes, the attachment upload becomes a cross-site request forgery target - and it carries
    /// <c>DisableAntiforgery()</c>, which it needs because the body is a raw stream that must not be
    /// buffered for model binding. Lax is what refuses the cookie on a cross-site POST and is therefore
    /// the only thing standing in for the antiforgery token that endpoint cannot have.
    /// <para>
    /// <c>HttpOnly</c> keeps it out of <c>document.cookie</c>, which also keeps it out of Safari's
    /// seven-day cap on script-set cookies - a server-set cookie is not subject to that, and a
    /// client-set one would quietly become a one-week session.
    /// </para>
    /// <para>
    /// <c>Secure</c> follows the scheme the <i>browser</i> used rather than being hard-coded on. This
    /// service is plain HTTP behind YARP, so <c>Request.IsHttps</c> is only right because
    /// <c>UseForwardedHeaders</c> runs first and honours <c>X-Forwarded-Proto</c>. Hard-coding it true
    /// would silently drop the cookie on the local HTTP profile (port 5242).
    /// </para>
    /// </remarks>
    private static void Write(HttpContext http, string token, DateTimeOffset expiresAt)
    {
        http.Response.Cookies.Append(AnonymousSessionOptions.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            Expires = expiresAt
        });
    }

    private static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>
    /// Base64url, because the value goes in a cookie and <c>+</c>, <c>/</c> and <c>=</c> all have to be
    /// quoted there. Hand-rolled rather than pulling in a dependency for three replacements.
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

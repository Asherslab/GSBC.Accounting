namespace GSBC.Accounting.Grpc.Features.Sessions;

/// <summary>
/// The names behind the anonymous-session authentication scheme, its claim and its policy.
/// </summary>
/// <remarks>
/// <b>"Anonymous session" is authentication of a browser, never of a person.</b> The scheme
/// authenticates the <c>__gsbc_anon</c> cookie, which answers exactly one question - "is this the
/// browser that saved that draft?" - and it answers it with a bearer token anybody holding the cookie
/// can present. Being authenticated under this scheme therefore says nothing about who somebody is.
/// <para>
/// <b>Nothing that needs to know who a person is may be hung off this scheme.</b> Not an approval, not
/// a finance step, not an audit trail naming somebody. Sections 7 and 8 of both paper forms are
/// completed by a human who is not the claimant, and that stays true. When real sign-in arrives it gets
/// its own scheme and its own policy alongside this one, and the two are told apart by
/// <see cref="AuthenticationScheme"/> rather than by inspecting claims.
/// </para>
/// </remarks>
public static class AnonymousSessionDefaults
{
    /// <summary>
    /// The authentication scheme. Also the <c>AuthenticationType</c> on the resulting identity, so
    /// <c>User.Identity.AuthenticationType</c> tells an anonymous session apart from a future signed-in
    /// user without reading claims.
    /// </summary>
    public const string AuthenticationScheme = "AnonymousSession";

    /// <summary>
    /// The session id, as a claim. Deliberately NOT <c>ClaimTypes.NameIdentifier</c>.
    /// </summary>
    /// <remarks>
    /// <b>The claim type is the guard rail.</b> <c>NameIdentifier</c> is what every library, log
    /// enricher and future authorisation handler reaches for when it wants "the user", and putting a
    /// browser-ownership token there would let a session id be mistaken for a person's id by code that
    /// never read this file. A bespoke type cannot be picked up by accident.
    /// </remarks>
    public const string SessionIdClaim = "gsbc:anonymous_session_id";
}

/// <summary>
/// Authorisation policy names.
/// </summary>
/// <remarks>
/// <b>Policies rather than bare <c>[Authorize]</c>, because the interesting question is coming.</b>
/// Today there is one kind of caller. When staff sign-in lands there will be two, and every endpoint
/// will have to say which kinds it accepts - anonymous only, signed-in only, or either. Naming the
/// policy now means that decision is made in one file per endpoint attribute rather than by retrofitting
/// authorisation onto endpoints that never had any.
/// </remarks>
public static class Policies
{
    /// <summary>
    /// Requires an authenticated anonymous session: a valid, unexpired <c>__gsbc_anon</c> cookie.
    /// </summary>
    /// <remarks>
    /// <b>This REQUIRES something. It is not ASP.NET Core's <c>[AllowAnonymous]</c> and is not a way to
    /// opt out of authorisation.</b> An endpoint carrying this policy refuses a browser that has never
    /// created a draft.
    /// <para>
    /// It is also only the floor. Satisfying it proves a session exists, never that the session owns the
    /// submission in the request - that is the <c>x.OwnerSessionId == sessionId</c> predicate in every
    /// query, and this policy does not replace a single one of them.
    /// </para>
    /// </remarks>
    public const string AnonymousSession = "AnonymousSession";
}

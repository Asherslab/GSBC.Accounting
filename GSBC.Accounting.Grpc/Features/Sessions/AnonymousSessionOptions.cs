namespace GSBC.Accounting.Grpc.Features.Sessions;

/// <summary>
/// The numbers behind the anonymous session, in one place because three of them are promises made to a
/// claimant rather than tuning knobs.
/// </summary>
public static class AnonymousSessionOptions
{
    /// <summary>
    /// Prefixed with two underscores to match GSBC.ImpactKids' <c>__gsbc_display</c>, so anything
    /// grepping for this app's cookies finds both.
    /// <para>
    /// Named for the session rather than for drafts - it was <c>__gsbc_drafts</c> until the session
    /// became an authenticated principal gating everything, not just the drafts list. Renamed before
    /// any deployment, so no live cookie was invalidated; renaming it later would silently log every
    /// claimant out of their saved drafts.
    /// </para>
    /// </summary>
    public const string CookieName = "__gsbc_anon";

    /// <summary>
    /// One year, which is as close to "indefinite" as a cookie is allowed to be.
    /// </summary>
    /// <remarks>
    /// <b>A longer number here would be a lie the browser quietly corrects.</b> Chrome caps cookie
    /// expiry at 400 days and other browsers have their own ceilings, so a ten-year <c>Max-Age</c>
    /// would be truncated without any error and the server would believe in a session the browser had
    /// already thrown away. A year, renewed, is the honest version of the same intent - and it is the
    /// number GSBC.ImpactKids' display cookie settled on for the same reason.
    /// </remarks>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    /// <summary>
    /// How much of the lifetime is spent before the next request renews it.
    /// </summary>
    /// <remarks>
    /// Half. Most claimants file a form once or twice a year, so the common visit is months after the
    /// last one: renewing only in the second half means a person who comes back at all effectively
    /// never expires, while a browser that is never seen again lets its session lapse on schedule.
    /// </remarks>
    public static readonly TimeSpan RenewAfter = TimeSpan.FromDays(180);

    /// <summary>
    /// How long an untouched draft survives before the purge soft-deletes it.
    /// </summary>
    /// <remarks>
    /// <b>This is a privacy limit, not a storage one.</b> A draft carries a claimant's name and contact
    /// details from the moment section 1 is typed, and an abandoned one is a form nobody will ever
    /// submit - keeping it for the seven years the ACNC asks of <i>submitted</i> records would be
    /// retaining personal data for a purpose that has ended. Ninety days is long enough that somebody
    /// who started a claim before a holiday still finds it.
    /// </remarks>
    public static readonly TimeSpan AbandonedDraftLifetime = TimeSpan.FromDays(90);
}

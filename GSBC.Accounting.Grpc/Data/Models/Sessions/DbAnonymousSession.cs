namespace GSBC.Accounting.Grpc.Data.Models.Sessions;

/// <summary>
/// One anonymous session: a browser's claim to its own drafts. The row a <c>__gsbc_anon</c> cookie
/// resolves to, and the principal the <c>AnonymousSession</c> scheme authenticates.
/// </summary>
/// <remarks>
/// <b>The cookie carries a random token and nothing else - no signature, no data protection payload.</b>
/// That is deliberate. An ASP.NET Core data-protection key ring lives in the container filesystem
/// unless it is explicitly persisted, so a pod restart would invalidate every cookie the app had ever
/// issued - and these are meant to last a year. A token looked up in this table survives a restart, a
/// redeploy and a scale-out, because the database is the only thing that has to remember it.
/// <para>
/// What the table buys beyond that: a session can be listed, renewed, expired and - once there are real
/// users - claimed, none of which a self-contained signed cookie allows.
/// </para>
/// <para>
/// <b>Only the hash is stored.</b> The raw token exists in the claimant's cookie jar and in the response
/// that set it, and nowhere else. A database backup, a log line or a `select *` therefore cannot be
/// replayed as somebody's session - which matters more here than it looks, because this token is the
/// only thing standing between a stranger and a half-filled reimbursement form carrying a claimant's
/// name and phone number.
/// </para>
/// </remarks>
public class DbAnonymousSession
{
    public required Guid Id { get; set; }

    /// <summary>
    /// SHA-256 of the cookie's token, lowercase hex, 64 characters. Unique, and the only column ever
    /// used to look a session up.
    /// </summary>
    public required string TokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Bumped when the session is renewed, not on every request. A write per request would turn every
    /// autosave keystroke into two round trips to Postgres for no information anyone reads.
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>
    /// When the cookie stops being accepted. Extended by <c>AnonymousSessions</c> once the session is past
    /// its halfway point - see <c>AnonymousSessionOptions.RenewAfter</c>.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Null for every session today, because nobody signs in yet.
    /// <para>
    /// <b>This is the whole of the upgrade path to real accounts, and it is why drafts are owned by a
    /// session rather than by a cookie value.</b> When sign-in arrives, a claimant's drafts are found
    /// through their sessions rather than moved: setting this column on the sessions a person signs in
    /// from makes every draft they ever saved on any of those browsers theirs, with no data migration
    /// and no ambiguity about which anonymous draft belonged to whom.
    /// </para>
    /// </summary>
    public Guid? UserId { get; set; }
}

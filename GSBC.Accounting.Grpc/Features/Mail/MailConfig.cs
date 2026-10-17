namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>
/// How the app sends email. Bound from the <c>Mail</c> section: the AppHost fills it locally, a ConfigMap
/// plus the <c>mail-secret</c> Secret fill it in the cluster.
/// </summary>
/// <remarks>
/// <b>Missing credentials do not stop the pod, unlike a missing attachment store.</b> Claims are still
/// accepted and their emails wait in <c>OutgoingEmails</c>; the moment the credentials arrive and the
/// pod restarts, the backlog goes out. Refusing to start would turn "the accounts team hears about this
/// claim an hour late" into "nobody can submit a claim at all".
/// </remarks>
public class MailConfig
{
    public const string SectionName = "Mail";

    public MailTransportKind Transport { get; set; } = MailTransportKind.None;

    // ---- Graph, Transport = Graph ----

    /// <summary>The Microsoft 365 tenant - "Directory (tenant) ID" on the app registration.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>"Application (client) ID" on the app registration.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret's <b>Value</b>, not its "Secret ID" - the portal shows both side by side, and
    /// the ID is the one that looks like a GUID and does not work.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    // ---- Directory, Transport = Directory ----

    /// <summary>Where the local transport writes each message. Development only.</summary>
    public string DirectoryPath { get; set; } = string.Empty;

    // ---- Who and where ----

    /// <summary>
    /// The mailbox every email is sent as, and whose Sent Items keep a copy. A shared mailbox, so it
    /// needs no licence and nobody's personal account is involved.
    /// </summary>
    public string Sender { get; set; } = "expenses@baptist.com.au";

    /// <summary>Where a submitted claim goes. The accounts team decides from there what reaches Xero.</summary>
    public string AccountsAddress { get; set; } = "accounts@baptist.com.au";

    /// <summary>
    /// The site's public address, for the PDF link in the accounts email. No trailing slash.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "https://expenses.baptist.com.au";

    /// <summary>
    /// The largest combined PDF that is attached rather than linked. 24 MB by default.
    /// </summary>
    /// <remarks>
    /// <b>Exchange's limits are on the encoded message, not the file.</b> An attachment travels as base64,
    /// a third larger again, and Exchange Online's default send and receive limits are 35 and 36 MB. 24 MB
    /// of PDF is about 33 MB on the wire, under both. A claim over this goes with the link alone and says
    /// so. Raising it means raising <c>MaxSendSize</c> on the sender and <c>MaxReceiveSize</c> on the
    /// accounts mailbox first - see <c>docs/modules/infrastructure/email.md</c>.
    /// </remarks>
    public long MaxAttachmentBytes { get; set; } = 24L * 1024 * 1024;

    public bool IsGraphConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(Sender);
}

public enum MailTransportKind
{
    /// <summary>Nothing is sent. Emails queue and wait.</summary>
    None,

    /// <summary>Microsoft Graph, as <see cref="MailConfig.Sender"/>.</summary>
    Graph,

    /// <summary>Each message written to <see cref="MailConfig.DirectoryPath"/>. For local development.</summary>
    Directory
}

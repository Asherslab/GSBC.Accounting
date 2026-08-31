namespace GSBC.Accounting.Grpc.Features.Attachments;

/// <summary>
/// Where the receipts live. Bound from the <c>Attachments</c> configuration section, which the AppHost
/// fills locally and a Kubernetes ConfigMap plus Secret fills in the cluster.
/// </summary>
/// <remarks>
/// <b>Production is one SeaweedFS with two buckets</b> — GSBC.ImpactKids' existing <c>photos</c> and
/// this app's <c>accounting</c> — so the deployed configuration differs from the local one only in
/// <see cref="ServiceUrl"/>. Locally this stack runs its own container on its own port so the two do
/// not interfere.
/// <para>
/// Two follow-ons live outside this repo and belong to whoever deploys it: the Backblaze backup
/// identity is bucket-scoped (<c>Read:photos</c>, <c>List:photos</c>) and needs <c>accounting</c>
/// added, with a second <c>rclone copy</c> in the CronJob; and <b>SeaweedFS reads its identities once
/// at startup</b>, so a changed Secret updates nothing until the pod restarts, with no error either way.
/// </para>
/// </remarks>
public class AttachmentStoreConfig
{
    public const string SectionName = "Attachments";

    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = "accounting";

    /// <summary>
    /// 20 MB. Sized for what this app actually stores - a scanned multi-page tax invoice, or a phone
    /// photo of a paper receipt - not for GSBC.ImpactKids' 1 MB ceiling, which was set for 30 KB JPEGs
    /// of faces.
    /// </summary>
    public long MaxBytesPerFile { get; set; } = 20L * 1024 * 1024;

    /// <summary>
    /// The cap that matters more than the per-file one. The upload endpoint is anonymous, so without a
    /// per-submission ceiling the object store is a free file host: create a draft, upload forever.
    /// </summary>
    public long MaxBytesPerSubmission { get; set; } = 100L * 1024 * 1024;

    public int MaxFilesPerSubmission { get; set; } = 25;

    /// <summary>
    /// Whether the AWS SDK may frame the body as <c>aws-chunked</c>. <b>False, and it must stay false.</b>
    /// </summary>
    /// <remarks>
    /// <b>Verified against this stack's SeaweedFS 3.98 on 2026-08-31, and the failure reproduces.</b>
    /// GSBC.ImpactKids hit it first; the scope doc asked for it to be checked rather than assumed,
    /// because a version bump could have fixed it. It has not.
    /// <para>
    /// With chunk encoding on, SeaweedFS stores the SDK's transfer framing <i>as the file</i>. A 701-byte
    /// PDF was stored as 996 bytes beginning <c>2BD;chunk-signature=045ae72…</c> and ending
    /// <c>x-amz-trailer-signature:408f2d0…</c>, with the real PDF in between. Nothing errors: the PUT
    /// succeeds, the row is valid, the recorded size and content type are right, and the object is not
    /// the file. Six years later that is an auditor's problem and nobody's explanation.
    /// </para>
    /// <para>
    /// The magic-byte check on read is what surfaced it here - the download answered 500 rather than
    /// handing over a corrupt receipt. Keep both: this flag stops it happening, and that check is how
    /// anyone would find out if it started again.
    /// </para>
    /// </remarks>
    public bool UseChunkEncoding { get; set; }
}

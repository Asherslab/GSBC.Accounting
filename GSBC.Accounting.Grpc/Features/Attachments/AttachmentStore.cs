using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;

namespace GSBC.Accounting.Grpc.Features.Attachments;

/// <summary>
/// The object store, seen from this app. Puts and gets receipt bytes; knows nothing about submissions.
/// </summary>
/// <remarks>
/// Deliberately unlike GSBC.ImpactKids' <c>PhotoStore</c>, which was designed for 30 KB JPEGs of
/// children's faces and whose choices mostly do not transfer: it buffers a whole body into a
/// <c>MemoryStream</c> before checking the length, reads objects into a <c>byte[]</c>, uses 12 hex
/// characters of hash (48 bits), hard-codes <c>.jpg</c> in the key, and stores no metadata at all. All
/// five are wrong for a 1-20 MB PDF under a seven-year retention obligation.
/// </remarks>
public class AttachmentStore(IAmazonS3 s3, AttachmentStoreConfig config, ILogger<AttachmentStore> logger)
{
    /// <summary>
    /// Reads the stream once: hashes it, sniffs its first bytes, and writes it to a temporary file.
    /// </summary>
    /// <remarks>
    /// A temporary file rather than memory. Twenty megabytes per request held in the managed heap is a
    /// denial-of-service invitation on an endpoint with no authentication, and S3 needs a seekable
    /// stream to sign the payload anyway.
    /// </remarks>
    public async Task<StagedUpload> StageAsync(Stream body, long maxBytes, CancellationToken token)
    {
        string tempPath = Path.GetTempFileName();

        try
        {
            byte[] head = new byte[FileSignature.BytesNeeded];
            int headFilled = 0;
            long total = 0;

            // IncrementalHash, not SHA256.Create(): SHA256 only offers one-shot ComputeHash, which
            // would mean holding the whole 20 MB in memory to hash it. This hashes as it streams.
            using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            await using (FileStream file = File.Create(tempPath))
            {
                byte[] buffer = new byte[81920];
                int read;

                while ((read = await body.ReadAsync(buffer, token)) > 0)
                {
                    total += read;

                    // Checked as it streams, not after. A Content-Length header is a claim by the
                    // caller; this is the enforcement, and it stops reading the moment the claim is
                    // exceeded rather than after the disk has taken the whole thing.
                    if (total > maxBytes)
                        return StagedUpload.TooLarge(tempPath, total);

                    if (headFilled < head.Length)
                    {
                        int copy = Math.Min(head.Length - headFilled, read);
                        buffer.AsSpan(0, copy).CopyTo(head.AsSpan(headFilled));
                        headFilled += copy;
                    }

                    sha.AppendData(buffer.AsSpan(0, read));
                    await file.WriteAsync(buffer.AsMemory(0, read), token);
                }
            }

            if (total == 0)
                return StagedUpload.Empty(tempPath);

            string? detected = FileSignature.Detect(head.AsSpan(0, headFilled));

            if (detected is null)
                return StagedUpload.UnsupportedType(tempPath, total);

            string hash = Convert.ToHexStringLower(sha.GetHashAndReset());

            return new StagedUpload
            {
                TempPath = tempPath,
                ByteSize = total,
                ContentHash = hash,
                DetectedContentType = detected,
                Outcome = StageOutcome.Ok
            };
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// The object key. Submission id then content hash, so the same file uploaded twice to the same
    /// submission is one object.
    /// </summary>
    /// <remarks>
    /// The <b>whole</b> SHA-256, not a prefix. ImpactKids keeps 12 hex characters — 48 bits, where a
    /// collision becomes likely around 16 million objects — which is fine for photos of a few hundred
    /// children and not fine for a store whose contents are financial evidence. Two different receipts
    /// colliding must be implausible, not merely unlikely.
    /// <para>
    /// The claimant's filename never appears in the key. It is attacker-controlled text, and it is kept
    /// as metadata for the reviewer instead.
    /// </para>
    /// </remarks>
    public static string KeyFor(Guid submissionId, string contentHash, string detectedContentType) =>
        $"submissions/{submissionId}/{contentHash}{FileSignature.ExtensionFor(detectedContentType)}";

    public async Task PutAsync(string key, StagedUpload staged, CancellationToken token)
    {
        await EnsureBucketAsync(token);

        await using FileStream file = File.OpenRead(staged.TempPath);

        PutObjectRequest request = new()
        {
            BucketName = config.BucketName,
            Key = key,
            InputStream = file,
            // The DETECTED type, never the declared one. Whatever the browser claimed has already done
            // its job as a cross-check; what gets stored is what the bytes actually are.
            ContentType = staged.DetectedContentType,
            // False. SeaweedFS 3.98 stores aws-chunked framing verbatim AS the file - measured on this
            // stack, see AttachmentStoreConfig.UseChunkEncoding for the bytes. Nothing errors when it
            // happens, which is what makes it dangerous.
            UseChunkEncoding = config.UseChunkEncoding
        };

        await s3.PutObjectAsync(request, token);
    }

    /// <summary>
    /// Opens the stored object, re-checking its first bytes before any of it reaches the caller.
    /// </summary>
    /// <remarks>
    /// The read-side magic-byte check is what makes a silently-corrupted object visible. It is the test
    /// that would have caught SeaweedFS storing <c>aws-chunked</c> framing verbatim: the row is valid,
    /// the size is right, and the object is not the file.
    /// </remarks>
    public async Task<AttachmentContent> GetAsync(string key, string expectedContentType, CancellationToken token)
    {
        GetObjectResponse response = await s3.GetObjectAsync(config.BucketName, key, token);

        Stream content = response.ResponseStream;

        byte[] head = new byte[FileSignature.BytesNeeded];
        int filled = await ReadAtLeastAsync(content, head, token);

        string? detected = FileSignature.Detect(head.AsSpan(0, filled));

        if (detected is null || detected != expectedContentType)
        {
            content.Dispose();

            // Loud, and with both types named. A quiet failure here is the one that costs an audit.
            logger.LogError(
                "Stored attachment {Key} does not match its recorded type. Recorded {Expected}, bytes say "
                + "{Detected}. The object store may have altered it on write",
                key, expectedContentType, detected ?? "unrecognised");

            return AttachmentContent.Corrupt();
        }

        // The sniffed bytes are put back in front, so the caller gets the whole object rather than one
        // missing its first sixteen bytes.
        return new AttachmentContent
        {
            Ok = true,
            Stream = new PrefixedStream(head.AsMemory(0, filled), content),
            ContentLength = response.ContentLength
        };
    }

    public async Task DeleteAsync(string key, CancellationToken token) =>
        await s3.DeleteObjectAsync(config.BucketName, key, token);

    /// <summary>Creates the bucket if it is not there. Safe to call repeatedly.</summary>
    public async Task EnsureBucketAsync(CancellationToken token = default)
    {
        if (await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3, config.BucketName))
            return;

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = config.BucketName }, token);
    }

    private static async Task<int> ReadAtLeastAsync(Stream stream, byte[] buffer, CancellationToken token)
    {
        int filled = 0;

        while (filled < buffer.Length)
        {
            // A single ReadAsync is allowed to return fewer bytes than asked for, and on a network
            // stream it usually does. Reading once and sniffing what came back would reject valid HEIC
            // files at random, because their brand sits at offset 8.
            int read = await stream.ReadAsync(buffer.AsMemory(filled), token);

            if (read == 0)
                break;

            filled += read;
        }

        return filled;
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // A leftover temp file is the operating system's problem, not a reason to fail an upload
            // that otherwise succeeded.
        }
    }
}

public enum StageOutcome
{
    Ok,
    TooLarge,
    Empty,
    UnsupportedType
}

public class StagedUpload
{
    public required string TempPath { get; init; }
    public required StageOutcome Outcome { get; init; }
    public long ByteSize { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public string DetectedContentType { get; init; } = string.Empty;

    public static StagedUpload TooLarge(string path, long size) =>
        new() { TempPath = path, Outcome = StageOutcome.TooLarge, ByteSize = size };

    public static StagedUpload Empty(string path) =>
        new() { TempPath = path, Outcome = StageOutcome.Empty };

    public static StagedUpload UnsupportedType(string path, long size) =>
        new() { TempPath = path, Outcome = StageOutcome.UnsupportedType, ByteSize = size };
}

public class AttachmentContent
{
    public bool Ok { get; init; }
    public Stream? Stream { get; init; }
    public long ContentLength { get; init; }

    public static AttachmentContent Corrupt() => new() { Ok = false };
}

/// <summary>
/// Replays a few already-read bytes, then continues from the underlying stream.
/// </summary>
/// <remarks>
/// Exists so the read-side signature check does not cost the caller the first bytes of their file. The
/// alternative — a second GET after sniffing — doubles the round trips to the store for every download.
/// </remarks>
public sealed class PrefixedStream(ReadOnlyMemory<byte> prefix, Stream inner) : Stream
{
    private int _prefixPosition;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (_prefixPosition < prefix.Length)
        {
            int take = Math.Min(buffer.Length, prefix.Length - _prefixPosition);
            prefix.Slice(_prefixPosition, take).CopyTo(buffer);
            _prefixPosition += take;

            return take;
        }

        return await inner.ReadAsync(buffer, token);
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();

        base.Dispose(disposing);
    }
}

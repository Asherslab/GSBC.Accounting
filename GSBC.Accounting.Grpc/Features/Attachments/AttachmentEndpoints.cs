using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Features.Attachments;

/// <summary>
/// Upload and download, as plain HTTP under <c>/api/</c>.
/// </summary>
/// <remarks>
/// <b>Not gRPC, deliberately.</b> A receipt is 1-20 MB and the gRPC channel should never carry file
/// bytes; YARP forwards <c>/api/</c> straight through.
/// <para>
/// <b>Both endpoints are anonymous</b>, and the submission id is the only credential either has - which
/// is why that id is a <c>Guid</c> and never a sequence. The upload is bounded on every axis that
/// matters: bytes per file, bytes per submission, files per submission, and a content type the bytes
/// themselves have to agree with.
/// </para>
/// <para>
/// Note what a client cannot do here: it cannot upload against a submission that does not exist, and it
/// cannot upload against one that is no longer a <c>Draft</c>. Together those make this an endpoint that
/// only ever adds evidence to a form somebody is in the middle of filling in.
/// </para>
/// </remarks>
public static class AttachmentEndpoints
{
    public static void AddAttachmentEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/submissions/{submissionId:guid}/attachments");

        group.MapPost("", UploadAsync)
            // The body is read as a stream, so model binding must not try to buffer or parse it first.
            .DisableAntiforgery()
            // The tightest limit in the app, because this one writes bytes to storage that nothing
            // authenticated is standing in front of.
            .RequireRateLimiting(RateLimiting.UploadPolicy);

        group.MapGet("{attachmentId:guid}", DownloadAsync)
            .RequireRateLimiting(RateLimiting.UploadPolicy);
    }

    private static async Task<IResult> UploadAsync(
        Guid submissionId,
        HttpRequest request,
        AccountingDbContext db,
        AttachmentStore store,
        AttachmentStoreConfig config,
        ILogger<AttachmentStore> logger,
        CancellationToken token
    )
    {
        string fileName = request.Query["fileName"].ToString();
        string declaredType = request.ContentType ?? string.Empty;

        if (!Enum.TryParse(request.Query["kind"].ToString(), ignoreCase: true, out AttachmentKind kind))
            kind = AttachmentKind.ItemisedReceipt;

        // Cheap rejection before a single byte is read. Content-Length is only a claim, so the real
        // enforcement is in StageAsync as it streams - but honouring an honest claim early saves
        // transferring 200 MB just to refuse it.
        if (request.ContentLength is { } declaredLength && declaredLength > config.MaxBytesPerFile)
            return TooLarge(config);

        DbExpenseSubmission? submission = await db.ExpenseSubmissions
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == submissionId, token);

        if (submission is null)
            return Results.NotFound(new { error = "No such submission." });

        // A submitted form is evidence, and evidence does not gain new pages afterwards.
        if (submission.Status != SubmissionStatus.Draft)
            return Results.Conflict(new { error = "This submission has already been submitted." });

        if (submission.Attachments.Count >= config.MaxFilesPerSubmission)
            return Results.BadRequest(new { error = $"A submission may carry at most {config.MaxFilesPerSubmission} files." });

        long alreadyStored = submission.Attachments.Sum(x => x.ByteSize);
        long remaining = config.MaxBytesPerSubmission - alreadyStored;

        if (remaining <= 0)
            return Results.BadRequest(new { error = "This submission has reached its total attachment size limit." });

        // The tighter of the two ceilings, so one huge file cannot exceed the per-submission budget and
        // a nearly-full submission cannot be topped up past it either.
        long ceiling = Math.Min(config.MaxBytesPerFile, remaining);

        StagedUpload staged = await store.StageAsync(request.Body, ceiling, token);

        try
        {
            switch (staged.Outcome)
            {
                case StageOutcome.TooLarge:
                    return TooLarge(config);

                case StageOutcome.Empty:
                    return Results.BadRequest(new { error = "That file is empty." });

                case StageOutcome.UnsupportedType:
                    return Results.BadRequest(new
                    {
                        error = "That file is not a PDF, JPEG, PNG, HEIC or WebP. A receipt has to be a "
                                + "document or a photo."
                    });
            }

            // The declared type must agree with the bytes. This is what refuses a .exe renamed to .pdf -
            // and, more usefully in practice, what catches a browser mislabelling a HEIC photo.
            if (!FileSignature.Matches(declaredType, staged.DetectedContentType))
            {
                logger.LogWarning(
                    "Refused an upload for submission {SubmissionId}: declared {Declared}, bytes say {Detected}",
                    submissionId, FileSignature.Normalise(declaredType), staged.DetectedContentType);

                return Results.BadRequest(new
                {
                    error = $"That file says it is {FileSignature.Normalise(declaredType)} but its contents "
                            + $"are {staged.DetectedContentType}."
                });
            }

            // Same bytes, same submission, same object. Uploading a receipt twice is a slip, not two
            // pieces of evidence.
            DbExpenseAttachment? existing = submission.Attachments
                .FirstOrDefault(x => x.ContentHash == staged.ContentHash);

            if (existing is not null)
                return Results.Ok(ToContract(existing));

            string key = AttachmentStore.KeyFor(submissionId, staged.ContentHash, staged.DetectedContentType);

            await store.PutAsync(key, staged, token);

            DbExpenseAttachment attachment = new()
            {
                Id = Guid.Empty,
                SubmissionId = submissionId,
                LineId = Guid.TryParse(request.Query["lineId"].ToString(), out Guid lineId) ? lineId : null,
                FileName = SafeFileName(fileName, staged.DetectedContentType),
                ContentType = staged.DetectedContentType,
                ByteSize = staged.ByteSize,
                ContentHash = staged.ContentHash,
                ObjectKey = key,
                Kind = kind,
                UploadedAt = DateTimeOffset.UtcNow
            };

            await db.ExpenseAttachments.AddAsync(attachment, token);
            await db.SaveChangesAsync(token);

            return Results.Ok(ToContract(attachment));
        }
        finally
        {
            AttachmentStore.TryDelete(staged.TempPath);
        }
    }

    private static async Task<IResult> DownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        AccountingDbContext db,
        AttachmentStore store,
        ILogger<AttachmentStore> logger,
        HttpResponse response,
        CancellationToken token
    )
    {
        DbExpenseAttachment? attachment = await db.ExpenseAttachments
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.SubmissionId == submissionId, token);

        if (attachment is null)
            return Results.NotFound();

        AttachmentContent content;

        try
        {
            content = await store.GetAsync(attachment.ObjectKey, attachment.ContentType, token);
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // The row exists and the object does not. That is a broken store, not a missing attachment,
            // and it must not hide behind a 404 - somebody has to notice that evidence has gone.
            logger.LogError(ex, "Attachment {Id} has a row but no object at {Key}", attachmentId, attachment.ObjectKey);

            return Results.Problem("The attachment store is missing this file.", statusCode: 500);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read attachment {Id} from the store", attachmentId);

            return Results.Problem("The attachment store could not be reached.", statusCode: 500);
        }

        if (!content.Ok)
            return Results.Problem("The stored file does not match what was recorded for it.", statusCode: 500);

        // Two headers, both load-bearing, because this serves user-supplied files from the app's own
        // origin. Without them a PDF or an HTML-ish file uploaded as a "receipt" renders in place and
        // becomes same-origin script. Serving your own re-encoded JPEGs, as GSBC.ImpactKids does, is not
        // this shape of problem - serving whatever a stranger uploaded is.
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ContentDisposition = $"attachment; filename=\"{SanitiseHeader(attachment.FileName)}\"";

        return Results.Stream(content.Stream!, attachment.ContentType);
    }

    private static IResult TooLarge(AttachmentStoreConfig config) =>
        Results.Json(
            new { error = $"That file is larger than {config.MaxBytesPerFile / (1024 * 1024)} MB." },
            statusCode: StatusCodes.Status413PayloadTooLarge);

    /// <summary>
    /// Keeps a readable name and drops anything that could be read as a path.
    /// </summary>
    /// <remarks>
    /// The filename never reaches the object key, so this is not the only defence - but it is written to
    /// a header and shown to a reviewer, and a name containing <c>../</c> or a newline is worth refusing
    /// on principle rather than reasoning about every consumer.
    /// </remarks>
    private static string SafeFileName(string? fileName, string detectedContentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return $"receipt{FileSignature.ExtensionFor(detectedContentType)}";

        string name = Path.GetFileName(fileName.Trim());

        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return name.Length > 260 ? name[^260..] : name;
    }

    /// <summary>
    /// Strips what would break, or forge, a header. A quoted filename containing a quote or a CR/LF is
    /// a response-splitting question, and the answer is simply not to put one there.
    /// </summary>
    private static string SanitiseHeader(string value) =>
        new(value.Where(c => c is not ('"' or '\\' or '\r' or '\n') && !char.IsControl(c)).ToArray());

    private static ExpenseAttachment ToContract(DbExpenseAttachment attachment) => new()
    {
        Id = attachment.Id,
        SubmissionId = attachment.SubmissionId,
        LineId = attachment.LineId,
        FileName = attachment.FileName,
        ContentType = attachment.ContentType,
        ByteSize = attachment.ByteSize,
        ContentHash = attachment.ContentHash,
        Kind = attachment.Kind,
        UploadedAt = attachment.UploadedAt.UtcDateTime
    };
}

using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Extensions;
using GSBC.Accounting.Grpc.Features.Sessions;
using Microsoft.AspNetCore.Authorization;
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
/// <b>Both endpoints check the <c>__gsbc_anon</c> cookie, and the submission id is no longer a
/// credential on its own.</b> Before that cookie existed, anyone holding an id could attach files to
/// somebody else's draft or download the receipts on it - and that id is printed on screen after a save
/// and baked into the PDF's filename, so it leaks by being shared rather than by being guessed. The
/// upload is still bounded on every other axis too: bytes per file, bytes per submission, files per
/// submission, and a content type the bytes themselves have to agree with.
/// </para>
/// <para>
/// <b>The two endpoints do not share one rule, and the difference is deliberate.</b> An upload is
/// owner-only. A download is allowed either to the owner or to anyone holding the id of a
/// <b>submitted</b> claim - because a submitted claim's evidence is what a reviewer is handed a link
/// to, and there is no approval screen in this scope to hand it to them any other way. A draft's
/// receipts are private to the person still filling the form in.
/// </para>
/// <para>
/// Note what a client cannot do here: it cannot upload against a submission that does not exist, that
/// it does not own, or that is no longer a <c>Draft</c>. Together those make this an endpoint that only
/// ever adds evidence to a form the caller is in the middle of filling in.
/// </para>
/// </remarks>
public static class AttachmentEndpoints
{
    public static void AddAttachmentEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/submissions/{submissionId:guid}/attachments")
            // Policy on the GROUP, so a route added here later inherits it rather than having to
            // remember it. The download opts back out below, out loud.
            .RequireAuthorization(Policies.AnonymousSession);

        group.MapPost("", UploadAsync)
            // The body is read as a stream, so model binding must not try to buffer or parse it first.
            .DisableAntiforgery()
            // The tightest limit in the app, because this one writes bytes to storage that nothing
            // authenticated is standing in front of.
            .RequireRateLimiting(RateLimiting.UploadPolicy);

        // THE ONE READ THAT LEAVES THE POLICY, and it is the same exemption the PDF endpoint takes.
        // A submitted claim's evidence has to be readable by whoever is handed its id, because that is
        // the only review path this scope has - there is no approval screen to hand a reviewer instead.
        // A DRAFT's receipts are still owner-only: the handler resolves the session itself and the
        // predicate below refuses a draft to anyone else. The exemption widens who may ask, never what
        // they get back.
        group.MapGet("{attachmentId:guid}", DownloadAsync)
            .RequireRateLimiting(RateLimiting.UploadPolicy)
            .AllowAnonymous();

        // Removing a receipt from a draft. This exists because drafts are resumable: without it the
        // attachments card could only forget a file on screen, and the next time the claimant opened
        // the draft it would be back - a page that ignores what somebody just did.
        group.MapDelete("{attachmentId:guid}", DetachAsync)
            .RequireRateLimiting(RateLimiting.UploadPolicy);

        // Correcting what a file was filed as. Picking the kind happens before the file is chosen, so
        // getting it wrong is easy - and without this the only remedy is to remove the receipt and
        // upload it again, which is a lot of ceremony for a mislabelled dropdown.
        group.MapPatch("{attachmentId:guid}/kind", RekindAsync)
            .RequireRateLimiting(RateLimiting.UploadPolicy);
    }

    private static async Task<IResult> UploadAsync(
        Guid submissionId,
        HttpRequest request,
        AccountingDbContext db,
        AnonymousSessions sessions,
        AttachmentStore store,
        AttachmentStoreConfig config,
        ILogger<AttachmentStore> logger,
        CancellationToken token
    )
    {
        string fileName = request.Query["fileName"].ToString();
        string declaredType = request.ContentType ?? string.Empty;

        if (!Enum.TryParse(request.Query["kind"].ToString(), ignoreCase: true, out AttachmentKind kind))
            kind = AttachmentKind.SupplierReceipt;

        // Which purchase this file evidences. The claimant's own key for the detail, not its row id -
        // Update rewrites the details on every autosave and a row id here would come unlinked within
        // seconds. Absent or unparseable means "this claim, purchase unstated", which is a state the
        // PDF prints rather than one anything refuses.
        Guid? detailKey = Guid.TryParse(request.Query["detailKey"].ToString(), out Guid parsedKey)
            ? parsedKey
            : null;

        // Cheap rejection before a single byte is read. Content-Length is only a claim, so the real
        // enforcement is in StageAsync as it streams - but honouring an honest claim early saves
        // transferring 200 MB just to refuse it.
        if (request.ContentLength is { } declaredLength && declaredLength > config.MaxBytesPerFile)
            return TooLarge(config);

        // Resolved before the body is touched. There is no point streaming 20 MB to disk to discover
        // the caller has no business writing here - and never mints a session, because a browser that
        // has not created a draft has nothing to attach a file to.
        Guid? sessionId = await sessions.CurrentAsync(token);

        DbExpenseSubmission? submission = sessionId is null
            ? null
            : await db.ExpenseSubmissions
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == submissionId && x.OwnerSessionId == sessionId, token);

        // The same answer for "no such submission", "not yours" and "no cookie". Distinguishing them
        // would turn this into a way to ask the server which submission ids are real.
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
            //
            // IGNOREQUERYFILTERS IS LOAD-BEARING, and the reason is the unique index on
            // (SubmissionId, ContentHash). That index does not know about soft deletes, so a claimant
            // who removes a receipt and then attaches the same file again would sail past a filtered
            // duplicate check and straight into a unique-constraint violation - a 500 for what is a
            // perfectly ordinary change of mind. Finding the flagged row and un-flagging it is both the
            // fix and the behaviour somebody re-attaching a file expects.
            DbExpenseAttachment? existing = await db.ExpenseAttachments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.SubmissionId == submissionId && x.ContentHash == staged.ContentHash, token);

            if (existing is not null)
            {
                if (existing.Deleted)
                {
                    existing.Deleted = false;
                    existing.Kind = kind;
                    // Re-attached, and possibly to a different purchase than last time. Whatever the
                    // upload says now is what it belongs to - carrying the old key across would file the
                    // file against a receipt the claimant has since deleted.
                    existing.DetailKey = detailKey;
                    existing.UploadedAt = DateTimeOffset.UtcNow;

                    await db.SaveChangesAsync(token);
                }

                return Results.Ok(ToContract(existing));
            }

            string key = AttachmentStore.KeyFor(submissionId, staged.ContentHash, staged.DetectedContentType);

            await store.PutAsync(key, staged, token);

            DbExpenseAttachment attachment = new()
            {
                Id = Guid.Empty,
                SubmissionId = submissionId,
                DetailKey = detailKey,
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

    /// <summary>
    /// Changes what one attachment on a draft is filed as.
    /// </summary>
    /// <remarks>
    /// <b>Only the label moves - the bytes, the hash and the object key are untouched.</b> This is a
    /// correction to the claimant's own description of a file, not a re-upload, so nothing about the
    /// evidence itself changes.
    /// <para>
    /// <b>Drafts only, owner only</b>, for the same reason the delete is: the kind is what Submit
    /// checks when it insists on an itemised receipt or tax invoice, so relabelling a submitted claim's
    /// evidence would be editing the thing a reviewer is reading.
    /// </para>
    /// </remarks>
    private static async Task<IResult> RekindAsync(
        Guid submissionId,
        Guid attachmentId,
        RekindRequest body,
        AccountingDbContext db,
        AnonymousSessions sessions,
        CancellationToken token
    )
    {
        if (!Enum.IsDefined(body.Kind))
            return Results.BadRequest(new { error = "That is not a kind of attachment." });

        Guid? sessionId = await sessions.CurrentAsync(token);

        DbExpenseSubmission? submission = sessionId is null
            ? null
            : await db.ExpenseSubmissions
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == submissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return Results.NotFound(new { error = "No such submission." });

        if (submission.Status != SubmissionStatus.Draft)
            return Results.Conflict(new { error = "This submission has already been submitted." });

        DbExpenseAttachment? attachment = submission.Attachments.FirstOrDefault(x => x.Id == attachmentId);

        if (attachment is null)
            return Results.NotFound(new { error = "No such attachment." });

        attachment.Kind = body.Kind;

        await db.SaveChangesAsync(token);

        return Results.Ok(ToContract(attachment));
    }

    /// <summary>
    /// Soft-deletes one attachment from a draft the caller owns.
    /// </summary>
    /// <remarks>
    /// <b>The object is not deleted from the store, and this is not an oversight.</b> Nothing in this
    /// app destroys uploaded bytes; the row is flagged, the global query filter stops every later read
    /// from seeing it, and the submission the reviewer eventually reads does not reference it. What the
    /// claimant is promised is that the file is no longer part of their claim, which is exactly what
    /// happens.
    /// <para>
    /// <b>Drafts only.</b> A submitted claim's evidence is fixed - somebody removing a receipt from a
    /// claim already under review would be removing evidence, and that needs a person rather than a
    /// button on a form.
    /// </para>
    /// </remarks>
    private static async Task<IResult> DetachAsync(
        Guid submissionId,
        Guid attachmentId,
        AccountingDbContext db,
        AnonymousSessions sessions,
        CancellationToken token
    )
    {
        Guid? sessionId = await sessions.CurrentAsync(token);

        DbExpenseSubmission? submission = sessionId is null
            ? null
            : await db.ExpenseSubmissions
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == submissionId && x.OwnerSessionId == sessionId, token);

        if (submission is null)
            return Results.NotFound(new { error = "No such submission." });

        if (submission.Status != SubmissionStatus.Draft)
            return Results.Conflict(new { error = "This submission has already been submitted." });

        DbExpenseAttachment? attachment = submission.Attachments.FirstOrDefault(x => x.Id == attachmentId);

        if (attachment is null)
            return Results.NotFound(new { error = "No such attachment." });

        attachment.Deleted = true;

        await db.SaveChangesAsync(token);

        return Results.NoContent();
    }

    /// <summary>
    /// Serves one attachment's bytes - as a download by default, or inline for the preview.
    /// </summary>
    /// <remarks>
    /// <b><c>?inline=1</c> is honoured for images and for nothing else.</b> The preview exists because
    /// details are per-receipt now and somebody with four photos of dockets has to be able to tell which
    /// is which without downloading all four - so the page shows the image in a modal, and an
    /// <c>&lt;img&gt;</c> cannot render a response marked <c>Content-Disposition: attachment</c>.
    /// <para>
    /// Widening that to every type would undo the reason the header is there. This origin serves
    /// whatever a stranger uploaded, and a PDF or an HTML-ish file rendered in place becomes same-origin
    /// script. An <c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c> under <c>nosniff</c> cannot -
    /// the browser is forbidden from re-interpreting it as anything else. The allowlist below is
    /// therefore the security boundary, not a convenience: it is on the DETECTED type, which is what the
    /// bytes were checked to be at upload rather than what the upload claimed.
    /// </para>
    /// </remarks>
    private static async Task<IResult> DownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        AccountingDbContext db,
        AnonymousSessions sessions,
        AttachmentStore store,
        ILogger<AttachmentStore> logger,
        HttpRequest request,
        HttpResponse response,
        CancellationToken token
    )
    {
        Guid? sessionId = await sessions.CurrentAsync(token);

        // THE OWNER, OR ANYONE HOLDING THE ID OF A SUBMITTED CLAIM. The second half is what keeps the
        // only review path this scope has working: somebody is handed a submission id and reads the
        // claim and its evidence. A draft has no reviewer yet, so its receipts are the claimant's alone.
        bool readable = await db.ExpenseSubmissions.AnyAsync(
            x => x.Id == submissionId
                 && (x.Status == SubmissionStatus.Submitted
                     || (sessionId != null && x.OwnerSessionId == sessionId)),
            token);

        if (!readable)
            return Results.NotFound();

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
        //
        // nosniff is unconditional. It is what makes the inline case below safe at all: it forbids the
        // browser from deciding an image/png is really something executable.
        response.Headers.XContentTypeOptions = "nosniff";

        bool inline = request.Query["inline"] == "1" && PreviewableInline.Contains(attachment.ContentType);

        response.Headers.ContentDisposition = inline
            ? $"inline; filename=\"{SanitiseHeader(attachment.FileName)}\""
            : $"attachment; filename=\"{SanitiseHeader(attachment.FileName)}\"";

        return Results.Stream(content.Stream!, attachment.ContentType);
    }

    /// <summary>
    /// The only content types <c>?inline=1</c> will serve inline. <b>An allowlist, and a short one.</b>
    /// </summary>
    /// <remarks>
    /// Raster image types that no browser will execute, under <c>nosniff</c>. Not <c>application/pdf</c>
    /// - a PDF is a scripting host, and one rendered same-origin is a stored XSS. Not
    /// <c>image/svg+xml</c> either, which is not accepted at upload but is worth naming here so nobody
    /// adds it to both lists at once. HEIC is left out because it is not a type browsers render anyway,
    /// so allowing it would buy a blank preview and one more type on this list.
    /// </remarks>
    private static readonly HashSet<string> PreviewableInline =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

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
        DetailKey = attachment.DetailKey,
        FileName = attachment.FileName,
        ContentType = attachment.ContentType,
        ByteSize = attachment.ByteSize,
        ContentHash = attachment.ContentHash,
        Kind = attachment.Kind,
        UploadedAt = attachment.UploadedAt.UtcDateTime
    };

    /// <summary>The one field a claimant may change about a file once it is stored.</summary>
    private record RekindRequest(AttachmentKind Kind);
}

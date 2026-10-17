using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Grpc.Features.Attachments;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using QuestPDF.Fluent;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// The whole claim as one PDF: the form, then every file of evidence, in the manifest's order.
/// </summary>
/// <remarks>
/// <b>One file, because the accounts team asked for one.</b> The claim goes to
/// <c>accounts@baptist.com.au</c>, and from there possibly into Xero's bills inbox - which reportedly
/// turns each attachment on an email into a draft bill of its own. A form plus four receipts as five
/// files would be five bills to merge back by hand. So the receipts are bound in rather than sent beside.
/// <para>
/// Photos are re-encoded through <see cref="EvidenceImage"/> - every format, HEIC included, comes out as
/// a JPEG sized for print. PDF receipts are merged page-for-page through QuestPDF's
/// <see cref="DocumentOperation"/>, which wraps qpdf and ships it in the same native package as Skia.
/// </para>
/// <para>
/// <b>A file that cannot be included never fails the claim.</b> It gets a page saying so, naming the file
/// and its hash, and the original is still in the object store. A store outage, an encrypted PDF or a
/// photo Magick cannot read is a reason to tell the reviewer, not a reason the accounts team never hears
/// about a submitted claim.
/// </para>
/// </remarks>
public class ClaimPdf(AttachmentStore store, ILogger<ClaimPdf> logger)
{
    /// <summary>
    /// The submission must be loaded with its details, items, attachments, attendees and missing-receipt
    /// declaration - the same includes the PDF endpoint has always used.
    /// </summary>
    public async Task<byte[]> RenderAsync(DbExpenseSubmission submission, CancellationToken token)
    {
        IReadOnlyList<SubmissionDocument.Evidence> evidence = SubmissionDocument.OrderedEvidence(submission);

        // Nothing to merge: skip the temp directory and qpdf entirely.
        if (evidence.Count == 0)
            return new SubmissionDocument(submission).GeneratePdf();

        string work = Path.Combine(Path.GetTempPath(), $"claim-{Guid.NewGuid():N}");
        Directory.CreateDirectory(work);

        try
        {
            string form = Path.Combine(work, "0-form.pdf");
            new SubmissionDocument(submission).GeneratePdf(form);

            List<string> parts = [];

            for (int i = 0; i < evidence.Count; i++)
            {
                SubmissionDocument.Evidence item = evidence[i];

                EvidencePage.Heading heading = new(i + 1, evidence.Count, submission.Reference, item.Purchase,
                    item.File.FileName, SubmissionDocument.EvidenceTypeLabel(item.File.Kind));

                parts.AddRange(await PartsForAsync(item.File, heading, work, i + 1, token));
            }

            DocumentOperation merged = DocumentOperation.LoadFile(form);

            foreach (string part in parts)
                merged = merged.MergeFile(part);

            string output = Path.Combine(work, "claim.pdf");
            merged.Save(output);

            return await File.ReadAllBytesAsync(output, token);
        }
        finally
        {
            try
            {
                Directory.Delete(work, recursive: true);
            }
            catch (Exception ex)
            {
                // A claimant's receipts left in /tmp until the pod restarts. Worth a warning, not worth
                // failing a render that has already succeeded.
                logger.LogWarning(ex, "Could not remove the claim PDF working directory {Path}", work);
            }
        }
    }

    /// <summary>
    /// The PDF files that represent one attachment, in order: a page for a photo; a heading page then
    /// the receipt's own pages for a PDF; a single explanatory page for anything that would not go in.
    /// </summary>
    private async Task<List<string>> PartsForAsync(
        DbExpenseAttachment file,
        EvidencePage.Heading heading,
        string work,
        int number,
        CancellationToken token
    )
    {
        string page = Path.Combine(work, $"{number}-page.pdf");

        try
        {
            AttachmentContent content = await store.GetAsync(file.ObjectKey, file.ContentType, token);

            if (!content.Ok || content.Stream is null)
                return [Unavailable(page, heading, file, "its stored bytes do not match the recorded file type")];

            await using Stream stream = content.Stream;

            if (file.ContentType == FileSignature.Pdf)
            {
                string original = Path.Combine(work, $"{number}-upload.pdf");
                string checkedCopy = Path.Combine(work, $"{number}-receipt.pdf");

                await using (FileStream target = File.Create(original))
                    await stream.CopyToAsync(target, token);

                // Opened and rewritten on its own first, so a PDF qpdf cannot read - encrypted, or
                // malformed past repair - fails here, against this one file, rather than halfway through
                // the merge of all of them.
                DocumentOperation.LoadFile(original).Save(checkedCopy);

                new EvidencePage(heading, null,
                        "The uploaded PDF follows on the next page, exactly as uploaded.")
                    .GeneratePdf(page);

                return [page, checkedCopy];
            }

            // The sniffing stream cannot seek, and Magick wants to read the whole image anyway.
            using MemoryStream buffer = new();
            await stream.CopyToAsync(buffer, token);
            buffer.Position = 0;

            new EvidencePage(heading, EvidenceImage.ToJpeg(buffer), null).GeneratePdf(page);

            return [page];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Could not include attachment {AttachmentId} ({ContentType}) in the claim PDF for submission "
                + "{SubmissionId}. A placeholder page was printed instead",
                file.Id, file.ContentType, file.SubmissionId);

            return [Unavailable(page, heading, file, "it could not be read when this PDF was made")];
        }
    }

    /// <summary>
    /// Named for what it is and who filed it, because it ends up in somebody's downloads folder next to
    /// twenty others. A submitted claim is named by its claim reference, which is the thing anybody
    /// looking for the file again will be holding; a draft has none, so it falls back to the first
    /// eight characters of the id - enough to tell two apart, and the whole one is printed inside the
    /// document either way.
    /// </summary>
    public static string FileName(DbExpenseSubmission submission)
    {
        string kind = submission.Kind == SubmissionKind.DebitCardPurchase
            ? "debit-card-purchase"
            : "expense-reimbursement";

        string who = new((submission.SubmitterName ?? "unnamed")
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        string tail = submission.Reference is { Length: > 0 } reference
            ? reference
            : submission.Id.ToString()[..8];

        return $"{kind}-{who}-{tail}.pdf";
    }

    private static string Unavailable(string path, EvidencePage.Heading heading, DbExpenseAttachment file,
        string why)
    {
        new EvidencePage(heading, null,
                $"This file could not be included, because {why}. The original is held in the Church's "
                + $"evidence store against this claim.\n\nSHA-256 {file.ContentHash}")
            .GeneratePdf(path);

        return path;
    }
}

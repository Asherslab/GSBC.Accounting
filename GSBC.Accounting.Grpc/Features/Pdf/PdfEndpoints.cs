using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Grpc.Features.Sessions;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// <c>GET /api/submissions/{id}/pdf</c>.
/// </summary>
/// <remarks>
/// <b>A submitted claim renders for anyone holding its id; a draft renders only for the session that
/// owns it.</b> The split is the point of this endpoint's authorisation rule and it is not a compromise
/// on the way to something stricter.
/// <para>
/// A submitted claim has to stay readable by id because that is the only review path this scope has:
/// there is no approval queue and no finance screen, so somebody is handed a submission id and reads
/// the PDF. Locking that to the claimant's own browser would leave the reviewer with <c>psql</c>. The
/// id is a <c>Guid</c> and never a sequence precisely because it carries that weight - a guessable id
/// would make every claim in the church readable by counting.
/// </para>
/// <para>
/// A <b>draft</b> is a different document. It is half-finished, nobody has reviewed it, and it already
/// carries a claimant's name, contact details and card last-four from the moment section 1 is typed.
/// Nobody but its author has any business reading one, and before the <c>__gsbc_anon</c> cookie
/// existed anybody holding the id could - including from the filename of a PDF that had been shared.
/// </para>
/// <para>
/// Served with the same two headers as an attachment download: <c>nosniff</c>, and
/// <c>Content-Disposition: attachment</c>. This document carries a claimant's name and contact details,
/// and rendering it inline from the app's own origin is a shape worth avoiding even for a file this app
/// generated itself.
/// </para>
/// </remarks>
public static class PdfEndpoints
{
    public static void AddPdfEndpoints(this WebApplication app)
    {
        app.MapGet("/api/submissions/{submissionId:guid}/pdf", async (
            Guid submissionId,
            AccountingDbContext db,
            AnonymousSessions sessions,
            HttpResponse response,
            CancellationToken token
        ) =>
        {
            // Never mints. Somebody following a link to a submitted claim's PDF is not creating a
            // draft, and handing them a year-long cookie for reading one document would be gratuitous.
            Guid? sessionId = await sessions.CurrentAsync(token);

            DbExpenseSubmission? submission = await db.ExpenseSubmissions
                .Include(x => x.Lines)
                .Include(x => x.Attachments)
                .Include(x => x.Attendees)
                .Include(x => x.Trips)
                .Include(x => x.MissingReceipt)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    x => x.Id == submissionId
                         && (x.Status == SubmissionStatus.Submitted
                             || (sessionId != null && x.OwnerSessionId == sessionId)),
                    token);

            // 404 rather than 403 for a draft somebody does not own, so the endpoint does not confirm
            // that an id exists to a caller who may not read it.
            if (submission is null)
                return Results.NotFound();

            byte[] pdf = new SubmissionDocument(submission).GeneratePdf();

            response.Headers.XContentTypeOptions = "nosniff";
            response.Headers.ContentDisposition =
                $"attachment; filename=\"{FileName(submission)}\"";

            return Results.File(pdf, "application/pdf");
        })
            // EXEMPT FROM Policies.AnonymousSession, deliberately, and this is the endpoint the
            // exemption exists for. A submitted claim's PDF is the only review path in this scope:
            // somebody is handed a submission id and reads it. Requiring a session would leave the
            // reviewer with psql. A draft is still owner-only - the predicate above is what enforces
            // that, and it is the reason this can be open without being a leak.
            .AllowAnonymous()
        // Rendering costs CPU rather than storage, so this has its own, lower ceiling.
        .RequireRateLimiting(RateLimiting.RenderPolicy);
    }

    /// <summary>
    /// Named for what it is and who filed it, because it ends up in somebody's downloads folder next to
    /// twenty others. The id is truncated to eight characters - enough to tell two apart, and the whole
    /// one is printed inside the document.
    /// </summary>
    private static string FileName(DbExpenseSubmission submission)
    {
        string kind = submission.Kind == Shared.Contracts.Entities.Features.Expenses.SubmissionKind.DebitCardPurchase
            ? "debit-card-purchase"
            : "expense-reimbursement";

        string who = new((submission.SubmitterName ?? "unnamed")
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        return $"{kind}-{who}-{submission.Id.ToString()[..8]}.pdf";
    }
}

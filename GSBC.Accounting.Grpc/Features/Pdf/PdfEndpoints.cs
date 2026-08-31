using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// <c>GET /api/submissions/{id}/pdf</c>.
/// </summary>
/// <remarks>
/// Anonymous, like everything else here, so <b>the submission id is the only credential</b> - which is
/// why it is a <c>Guid</c> and never a sequence. A guessable id would make every claim in the church
/// readable by counting.
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
            HttpResponse response,
            CancellationToken token
        ) =>
        {
            DbExpenseSubmission? submission = await db.ExpenseSubmissions
                .Include(x => x.Lines)
                .Include(x => x.Attachments)
                .Include(x => x.Attendees)
                .Include(x => x.Trips)
                .Include(x => x.MissingReceipt)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == submissionId, token);

            if (submission is null)
                return Results.NotFound();

            byte[] pdf = new SubmissionDocument(submission).GeneratePdf();

            response.Headers.XContentTypeOptions = "nosniff";
            response.Headers.ContentDisposition =
                $"attachment; filename=\"{FileName(submission)}\"";

            return Results.File(pdf, "application/pdf");
        })
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

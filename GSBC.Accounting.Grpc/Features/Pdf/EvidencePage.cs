using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// One page of the evidence that follows the form: a heading naming the file and its purchase, then
/// either the photo, or a note that the uploaded PDF follows, or a note that the file could not be
/// included.
/// </summary>
/// <remarks>
/// <b>The heading is the point.</b> A reviewer flicking through twelve pages of receipts needs every page
/// to say which numbered purchase in section 3 it evidences without turning back to the manifest - and
/// the file name alone is no help, because <c>IMG_4471.jpeg</c> reliably is what it is.
/// </remarks>
public class EvidencePage(EvidencePage.Heading heading, byte[]? jpeg, string? note) : IDocument
{
    public sealed record Heading(int Number, int Of, string? Reference, int? Purchase, string FileName,
        string Kind);

    public DocumentMetadata GetMetadata() => new();

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(14, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

            page.Header().Column(column =>
            {
                column.Item().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                    t.Span($"EVIDENCE {heading.Number} OF {heading.Of}");

                    if (heading.Reference is { Length: > 0 } reference)
                        t.Span($"   ·   Claim {reference}");
                });

                column.Item().PaddingTop(2).Text(heading.Purchase is { } purchase
                        ? $"Purchase {purchase} — {heading.Kind}"
                        : $"Not filed against a purchase — {heading.Kind}")
                    .FontSize(12).Bold();

                column.Item().Text(heading.FileName).FontSize(8).FontColor(Colors.Grey.Darken2);
                column.Item().PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Medium);
            });

            page.Content().PaddingTop(8).Element(content =>
            {
                if (jpeg is not null)
                {
                    // FitArea keeps the aspect ratio and fills whichever dimension runs out first, so a
                    // long thin docket and a landscape invoice both use the page.
                    content.AlignCenter().Image(jpeg).FitArea();
                    return;
                }

                content.Text(note ?? "").FontSize(9).FontColor(Colors.Grey.Darken3);
            });
        });
    }
}

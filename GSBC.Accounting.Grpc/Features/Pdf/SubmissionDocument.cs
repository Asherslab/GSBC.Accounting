using System.Globalization;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GSBC.Accounting.Grpc.Features.Pdf;

/// <summary>
/// Renders one submission as the printed form, plus a manifest of its evidence.
/// </summary>
/// <remarks>
/// <b>Rendered from the aggregate, not from the HTML page.</b> The screen layout and the printed layout
/// are different problems and are allowed to diverge - the screen has reveals, running totals and a
/// disabled approval section, and the paper needs ruled blocks somebody signs with a pen.
/// <para>
/// Sections 7 and 8 render as <b>empty ruled blocks</b>, matching the paper form, because an approver
/// and finance complete them by hand.
/// </para>
/// <para>
/// QuestPDF rather than filling the original <c>.docx</c> via OpenXML: OpenXML cannot render, so that
/// route needs headless LibreOffice - slow, font-fragile, and it drifts in layout. It is also moot, as
/// neither <c>.docx</c> contains a single form control: all 110 checkboxes are the literal character
/// U+2610, so there is no field mapping to preserve.
/// </para>
/// </remarks>
public class SubmissionDocument(DbExpenseSubmission submission) : IDocument
{
    private static readonly CultureInfo Australia = CultureInfo.GetCultureInfo("en-AU");

    private readonly PdfText _text = SubmissionPdfWording.For(submission.Kind);

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_text.DocumentTitle} — {submission.SubmitterName}",
        Author = "Good Shepherd Baptist Church"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(14, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

            page.Header().Element(Masthead);
            page.Content().PaddingVertical(8).Element(Body);

            page.Footer().Column(column =>
            {
                column.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Medium);
                column.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text(_text.Footer).FontSize(7).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(70).AlignRight().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });
        });
    }

    private void Masthead(IContainer container) =>
        container.Column(column =>
        {
            column.Item().Text("GOOD SHEPHERD BAPTIST CHURCH  |  FINANCE")
                .FontSize(7.5f).LetterSpacing(0.08f).FontColor(Colors.Grey.Darken1);

            column.Item().PaddingTop(2).Text(_text.DocumentTitle).FontSize(15).Bold();
            column.Item().Text(_text.DocumentSubtitle).FontSize(8).FontColor(Colors.Grey.Darken2);

            // The reference is the submission id, because it is the only identifier this app has - and
            // it is what a reviewer types back into a query.
            column.Item().PaddingTop(3).Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                t.Span("Reference ");
                t.Span(submission.Id.ToString()).FontFamily(Fonts.Consolas);
                t.Span($"   ·   Status {submission.Status}");

                if (submission.SubmittedAt is { } submitted)
                    t.Span($"   ·   Submitted {submitted.ToLocalTime():d MMMM yyyy, h:mm tt}");
            });

            // Loud, because a demonstration row must never be mistaken for a claim - least of all once
            // somebody has printed it.
            if (submission.IsMockData)
            {
                column.Item().PaddingTop(4).Background(Colors.Amber.Lighten4)
                    .Border(1).BorderColor(Colors.Amber.Darken2).Padding(4)
                    .Text("MOCK DATA — this is a generated demonstration submission, not a real claim.")
                    .FontSize(8).Bold().FontColor(Colors.Amber.Darken4);
            }

            column.Item().PaddingTop(4).BorderBottom(1.5f).BorderColor(Colors.Black);
        });

    private void Body(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(9);

            column.Item().Element(Section1);
            column.Item().Element(Section2);
            column.Item().Element(Section3);
            column.Item().Element(Section4);

            if (submission.MissingReceipt is not null)
                column.Item().Element(Section5);

            column.Item().Element(Section6);
            column.Item().Element(Section7);
            column.Item().Element(Section8);
            column.Item().Element(EvidenceManifest);
        });

    private void Section1(IContainer container) =>
        Card(container, _text.Section1Caption, inner => inner.Column(column =>
        {
            column.Spacing(3);

            if (submission.Kind == SubmissionKind.DebitCardPurchase)
            {
                column.Item().Row(row =>
                {
                    Field(row, _text.SubmitterNameLabel, submission.SubmitterName, 3);
                    Field(row, _text.FormDateLabel, Date(submission.FormDate), 2);
                    Field(row, "Card last 4 digits", submission.CardLastFourDigits, 2);
                });
                column.Item().Row(row =>
                {
                    Field(row, "Role / relationship", Role(), 2);
                    Field(row, "Ministry / department", submission.MinistryDepartment, 2);
                    Field(row, "Transaction date", Date(submission.TransactionDate), 2);
                    Field(row, "Time", submission.TransactionTime, 1);
                });
                column.Item().Row(row =>
                {
                    Field(row, "Supplier / merchant", submission.SupplierMerchant, 3);
                    Field(row, "Amount charged", Money(submission.AmountCharged), 2);
                    Field(row, "Bank reference", submission.BankReference, 2);
                });
            }
            else
            {
                column.Item().Row(row =>
                {
                    Field(row, _text.SubmitterNameLabel, submission.SubmitterName, 3);
                    Field(row, _text.FormDateLabel, Date(submission.FormDate), 2);
                    Field(row, "Role / relationship", Role(), 2);
                });
                column.Item().Row(row =>
                {
                    Field(row, "Phone / email", submission.ContactPhoneEmail, 3);
                    Field(row, "Ministry / department", submission.MinistryDepartment, 2);
                    Field(row, "Expense period",
                        $"{Date(submission.ExpensePeriodFrom)} to {Date(submission.ExpensePeriodTo)}", 2);
                });
                column.Item().Row(row =>
                {
                    Field(row, "Payment method",
                        submission.PaymentMethod is null ? null
                        : submission.PaymentMethod == PaymentMethod.Eft ? "EFT"
                        : $"Other: {submission.PaymentMethodOther}", 2);
                    Field(row, "Bank details on file", Tick(submission.BankDetailsOnFile), 2);
                    row.RelativeItem(3);
                });
            }

            // Reproduced on the printed form as well as on screen. It is the instruction that makes the
            // four-digit rule a rule rather than a preference.
            if (submission.Kind == SubmissionKind.DebitCardPurchase)
            {
                column.Item().PaddingTop(3).Text(
                        "Card security: Record only the last four digits. Never record the full card number, "
                        + "PIN or security code on this form.")
                    .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken2);
            }
        }));

    private void Section2(IContainer container) =>
        Card(container, _text.Section2Caption, inner => inner.Column(column =>
        {
            column.Spacing(3);

            column.Item().Row(row =>
            {
                Field(row, _text.PurposeActivityLabel, submission.PurposeActivity, 2);
                Field(row, "Event / project", submission.EventProject, 2);
                Field(row, "Prior approval by", submission.PriorApprovalBy, 2);
                Field(row, "Approval date", Date(submission.ApprovalDate), 1);
            });

            column.Item().PaddingTop(3).Text(_text.PurposeNarrativePrompt)
                .FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            column.Item().PaddingTop(1).MinHeight(30).Border(0.5f).BorderColor(Colors.Grey.Medium)
                .Padding(4).Text(submission.PurposeNarrative ?? "").FontSize(8.5f);
        }));

    private void Section3(IContainer container) =>
        Card(container, _text.Section3Caption, inner => inner.Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(2.6f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    Th(header, _text.LineColumn1Header);
                    Th(header, _text.LineColumn2Header);
                    Th(header, _text.LineColumn3Header);
                    Th(header, "Receipt");
                    Th(header, "Gross incl. GST", true);
                    Th(header, "GST shown", true);
                    Th(header, "Church use %", true);
                });

                foreach (DbExpenseLine line in submission.Lines.OrderBy(x => x.Ordinal))
                {
                    Td(table, submission.Kind == SubmissionKind.DebitCardPurchase
                        ? line.ItemDescription
                        : Date(line.LineDate));
                    Td(table, line.Details);
                    Td(table, line.Purpose);
                    Td(table, line.Evidence.ToString());
                    Td(table, Money(line.GrossAmount), true);
                    Td(table, Money(line.GstAmount), true);
                    Td(table, $"{line.ChurchUsePercent:0.##}%", true);
                }
            });

            column.Item().PaddingTop(5).AlignRight().Width(230).Column(totals =>
            {
                TotalRow(totals, _text.GrossTotalLabel, Money(submission.GrossTotal));
                TotalRow(totals, "GST shown on evidence", Money(submission.GstTotal));
                TotalRow(totals, _text.LessPersonalLabel, Money(submission.LessPersonalAmount));
                totals.Item().PaddingVertical(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
                TotalRow(totals, _text.NetTotalLabel, Money(submission.NetTotal), true);
            });
        }));

    private void Section4(IContainer container) =>
        Card(container, _text.Section4Caption, inner => inner.Column(column =>
        {
            column.Spacing(2);

            bool?[] answers =
            [
                submission.ComplianceQ1, submission.ComplianceQ2, submission.ComplianceQ3,
                submission.ComplianceQ4, submission.ComplianceQ5, submission.ComplianceQ6
            ];

            for (int i = 0; i < _text.ComplianceQuestions.Count; i++)
            {
                int index = i;

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"{index + 1}. {_text.ComplianceQuestions[index]}").FontSize(8);
                    row.ConstantItem(74).AlignRight().Text(Tick(answers[index]))
                        .FontSize(8).Bold()
                        // Unanswered is printed as "Not answered", never as a blank. A blank on paper is
                        // indistinguishable from a No, and the two are different facts.
                        .FontColor(answers[index] is null ? Colors.Red.Darken2
                            : answers[index] == true ? Colors.Orange.Darken3 : Colors.Black);
                });
            }

            if (submission.Attendees.Count > 0)
            {
                column.Item().PaddingTop(5).Text(_text.DetailTableCaption).FontSize(7.5f).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.4f); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(1.2f); c.RelativeColumn(1.2f); c.RelativeColumn(3);
                    });
                    table.Header(h =>
                    {
                        Th(h, "Date"); Th(h, "Person / recipient"); Th(h, "Relationship / role");
                        Th(h, "Amount", true); Th(h, "Private share", true); Th(h, "Reason and Church purpose");
                    });
                    foreach (DbExpenseAttendee a in submission.Attendees.OrderBy(x => x.Ordinal))
                    {
                        Td(table, Date(a.Date)); Td(table, a.Person); Td(table, a.Relationship);
                        Td(table, Money(a.Amount), true); Td(table, Money(a.PrivateShare), true); Td(table, a.Reason);
                    }
                });
            }

            if (submission.Trips.Count > 0)
            {
                column.Item().PaddingTop(5).Text(_text.DetailTableCaption).FontSize(7.5f).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.4f); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(1.2f); c.RelativeColumn(1.3f); c.RelativeColumn(3);
                    });
                    table.Header(h =>
                    {
                        Th(h, "Date"); Th(h, "From"); Th(h, "To");
                        Th(h, "Business km", true); Th(h, "Approved rate", true); Th(h, "Church purpose");
                    });
                    foreach (DbExpenseTrip t in submission.Trips.OrderBy(x => x.Ordinal))
                    {
                        Td(table, Date(t.Date)); Td(table, t.From); Td(table, t.To);
                        Td(table, t.BusinessKm?.ToString("0.#"), true);
                        Td(table, t.ApprovedRate?.ToString("0.000"), true);
                        Td(table, t.Purpose);
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(submission.ComplianceDetails))
            {
                column.Item().PaddingTop(5).Text(_text.ComplianceDetailsCaption).FontSize(7.5f).Bold();
                column.Item().MinHeight(24).Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(4)
                    .Text(submission.ComplianceDetails).FontSize(8);
            }
        }));

    private void Section5(IContainer container) =>
        Card(container, "5. MISSING RECEIPT DECLARATION", inner => inner.Column(column =>
        {
            DbMissingReceiptDeclaration missing = submission.MissingReceipt!;

            column.Item().Row(row =>
            {
                Field(row, "Supplier", missing.Supplier, 3);
                Field(row, "Date", Date(missing.Date), 2);
                Field(row, "Amount", Money(missing.Amount), 2);
            });

            column.Item().PaddingTop(3)
                .Text("Reason evidence cannot be supplied and steps taken to obtain a copy")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            column.Item().MinHeight(24).Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(4)
                .Text(missing.Reason ?? "").FontSize(8);

            column.Item().PaddingTop(4).Text($"{Box(missing.Declared)}  {_text.MissingReceiptDeclaration}")
                .FontSize(8);
        }));

    private void Section6(IContainer container) =>
        Card(container, _text.Section6Caption, inner => inner.Column(column =>
        {
            column.Spacing(3);

            bool?[] agreed =
            [
                submission.Declaration1, submission.Declaration2, submission.Declaration3,
                submission.Declaration4, submission.Declaration5
            ];

            for (int i = 0; i < _text.Declarations.Count; i++)
            {
                int index = i;
                column.Item().Text($"{Box(agreed[index] == true)}  {_text.Declarations[index]}").FontSize(8);
            }

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem(3).Column(c =>
                {
                    // The typed name is printed in an italic serif above a rule, so it reads as a
                    // signature block rather than as one more data field.
                    c.Item().Text(submission.SignatureName ?? "").FontSize(13).Italic().FontFamily(Fonts.Georgia);
                    c.Item().PaddingTop(1).BorderBottom(0.75f).BorderColor(Colors.Black);
                    c.Item().PaddingTop(2).Text(_text.SignatureLabel).FontSize(7).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(16);
                row.RelativeItem(1).Column(c =>
                {
                    c.Item().Text(Date(submission.SignedAt ?? submission.FormDate)).FontSize(9);
                    c.Item().PaddingTop(1).BorderBottom(0.75f).BorderColor(Colors.Black);
                    c.Item().PaddingTop(2).Text("Date").FontSize(7).FontColor(Colors.Grey.Darken2);
                });
            });
        }));

    /// <summary>
    /// Empty ruled blocks, matching the paper form. An approver who is <b>not the claimant</b> completes
    /// this with a pen; nothing in this scope can fill it in, because nothing here knows who anybody is.
    /// </summary>
    private void Section7(IContainer container) =>
        Card(container, "7. INDEPENDENT APPROVAL", inner => inner.Column(column =>
        {
            column.Item().Text("To be completed by an approver who is not the claimant.")
                .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken2);

            column.Item().PaddingTop(6).Row(row =>
            {
                Rule(row, "Approved by", 3);
                Rule(row, "Approval date", 2);
                Rule(row, _text.ApprovedAmountLabel, 2);
            });

            column.Item().PaddingTop(8).Text(
                    $"Decision:    {Box(false)} Approved      {Box(false)} Declined      "
                    + $"{Box(false)} {_text.ThirdDecisionLabel}")
                .FontSize(8);

            column.Item().PaddingTop(8).Row(row => Rule(row, "Approver signature", 1));
        }));

    private void Section8(IContainer container) =>
        Card(container, _text.Section8Caption, inner => inner.Column(column =>
        {
            column.Item().PaddingTop(4).Row(row =>
            {
                foreach (string field in _text.FinanceFields)
                    Rule(row, field, 1);
            });
        }));

    /// <summary>
    /// A manifest of the evidence, rather than the receipts themselves.
    /// </summary>
    /// <remarks>
    /// The scope doc's first pass: images embed directly but PDF receipts need page-level merging, which
    /// QuestPDF does not do, so appending a scanned tax invoice needs a separate merge step. Until that
    /// lands, the manifest gives an auditor what actually matters - the filename, type, size and
    /// <b>content hash</b> of every file, so "is this the file that was uploaded" has an answer that does
    /// not depend on the object store.
    /// </remarks>
    private void EvidenceManifest(IContainer container) =>
        Card(container, "EVIDENCE ATTACHED", inner => inner.Column(column =>
        {
            if (submission.Attachments.Count == 0)
            {
                column.Item().Text("No files attached.").FontSize(8).FontColor(Colors.Red.Darken2);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(1.6f);
                    c.RelativeColumn(1.2f); c.RelativeColumn(4);
                });
                table.Header(h =>
                {
                    Th(h, "File"); Th(h, "Evidence type"); Th(h, "Format");
                    Th(h, "Size", true); Th(h, "SHA-256");
                });

                foreach (DbExpenseAttachment a in submission.Attachments.OrderBy(x => x.UploadedAt))
                {
                    Td(table, a.FileName);
                    Td(table, a.Kind.ToString());
                    Td(table, a.ContentType);
                    Td(table, $"{a.ByteSize / 1024.0:0.#} KB", true);
                    Td(table, a.ContentHash);
                }
            });

            column.Item().PaddingTop(3).Text(
                    "The files themselves are held in the Church's evidence store against this submission's "
                    + "reference. Each hash above identifies exactly the bytes that were uploaded.")
                .FontSize(7).FontColor(Colors.Grey.Darken1);
        }));

    // ---- small builders -------------------------------------------------------------------------------

    private static void Card(IContainer container, string caption, Action<IContainer> body) =>
        container.Border(0.75f).BorderColor(Colors.Grey.Darken1).Column(column =>
        {
            column.Item().Background(Colors.Grey.Lighten3).Padding(4)
                .Text(caption).FontSize(8).Bold().LetterSpacing(0.04f);
            column.Item().Padding(6).Element(body);
        });

    private static void Field(RowDescriptor row, string label, string? value, uint size) =>
        row.RelativeItem(size).PaddingRight(6).Column(column =>
        {
            column.Item().Text(label).FontSize(6.5f).FontColor(Colors.Grey.Darken2).LetterSpacing(0.03f);
            column.Item().Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(8.5f);
            column.Item().PaddingTop(1).BorderBottom(0.4f).BorderColor(Colors.Grey.Medium);
        });

    /// <summary>An empty ruled block for somebody to write on.</summary>
    private static void Rule(RowDescriptor row, string label, uint size) =>
        row.RelativeItem(size).PaddingRight(8).Column(column =>
        {
            column.Item().Height(14);
            column.Item().BorderBottom(0.75f).BorderColor(Colors.Black);
            column.Item().PaddingTop(2).Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
        });

    private static void Th(TableCellDescriptor header, string text, bool right = false) =>
        header.Cell().Background(Colors.Grey.Lighten3).Border(0.4f).BorderColor(Colors.Grey.Medium)
            .Padding(3).Alignment(right)
            .Text(text).FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken3);

    private static void Td(TableDescriptor table, string? text, bool right = false) =>
        table.Cell().Border(0.4f).BorderColor(Colors.Grey.Medium).Padding(3).Alignment(right)
            .Text(text ?? "").FontSize(7.5f);

    private static void TotalRow(ColumnDescriptor column, string label, string value, bool strong = false) =>
        column.Item().Row(row =>
        {
            // QuestPDF's Bold() takes no argument, so the weight is chosen by branching rather than by
            // passing a flag into it.
            TextSpanDescriptor labelSpan = row.RelativeItem().Text(label).FontSize(strong ? 8.5f : 8);
            TextSpanDescriptor valueSpan = row.ConstantItem(74).AlignRight().Text(value)
                .FontSize(strong ? 9.5f : 8.5f).FontFamily(Fonts.Consolas);

            if (!strong)
                return;

            labelSpan.Bold();
            valueSpan.Bold();
        });

    private string? Role() => submission.Role switch
    {
        null => null,
        ClaimantRole.ResponsiblePerson => "Responsible Person",
        ClaimantRole.Other => $"Other: {submission.RoleOther}",
        _ => submission.Role.ToString()
    };

    /// <summary>Unanswered prints as words, never as a blank - a blank cannot be told apart from a No.</summary>
    private static string Tick(bool? value) => value switch
    {
        true => "YES",
        false => "No",
        null => "Not answered"
    };

    private static string Box(bool ticked) => ticked ? "[X]" : "[ ]";

    private static string Date(DateTimeOffset? value) =>
        value is null ? "—" : value.Value.ToLocalTime().ToString("d MMMM yyyy", Australia);

    private static string Money(decimal? value) =>
        value is null ? "—" : value.Value.ToString("C2", Australia);
}

file static class AlignmentExtensions
{
    /// <summary>Right-aligns a money column without repeating the ternary at twenty call sites.</summary>
    public static IContainer Alignment(this IContainer container, bool right) =>
        right ? container.AlignRight() : container;
}

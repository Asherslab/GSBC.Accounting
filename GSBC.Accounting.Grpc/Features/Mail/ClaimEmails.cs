using System.Globalization;
using System.Net;
using System.Text;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Grpc.Features.Pdf;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>
/// The two emails a submitted claim produces: the claim to the accounts team, and the receipt to the
/// claimant.
/// </summary>
/// <remarks>
/// <b>Every value from the form is HTML-encoded.</b> The claimant types the name, the supplier and the
/// purpose, and all of it lands in an HTML body read by the accounts team. Unencoded, a "name" is markup
/// in the treasurer's inbox.
/// </remarks>
public class ClaimEmails(MailConfig config, ClaimPdf claimPdf)
{
    private static readonly CultureInfo Australia = CultureInfo.GetCultureInfo("en-AU");

    /// <summary>
    /// To accounts, with the combined PDF, and <c>Reply-To</c> set to the claimant - so a question about
    /// the claim goes straight to the person who can answer it, not to an unattended shared mailbox.
    /// </summary>
    public async Task<OutgoingMessage> ClaimToAccountsAsync(DbExpenseSubmission submission, CancellationToken token)
    {
        byte[] pdf = await claimPdf.RenderAsync(submission, token);
        bool attach = pdf.LongLength <= config.MaxAttachmentBytes;

        string link = $"{config.PublicBaseUrl.TrimEnd('/')}/api/submissions/{submission.Id}/pdf";

        StringBuilder html = new();
        html.Append(Open());

        if (submission.IsMockData)
            html.Append(P("<strong>MOCK DATA — a generated demonstration claim, not a real one.</strong>"));

        html.Append(P($"A new {E(KindName(submission.Kind))} has been submitted."));

        html.Append("<table cellpadding=\"4\" style=\"border-collapse:collapse\">");
        Row(html, "Claim reference", submission.Reference);
        Row(html, "Claimant", submission.SubmitterName);
        Row(html, "Email", submission.SubmitterEmail);

        if (submission.Kind == SubmissionKind.ExpenseReimbursement)
        {
            Row(html, "Phone", submission.ContactPhone);
            Row(html, "Ministry / department", submission.MinistryDepartment);
            Row(html, "Bank details on file", submission.BankDetailsOnFile switch
            {
                true => "Yes",
                false => "No — the claimant was told to email them to accounts",
                null => "Not answered"
            });
        }
        else
        {
            Row(html, "Card last 4 digits", submission.CardLastFourDigits);
        }

        Row(html, NetLabel(submission.Kind), Money(submission.NetTotal));
        Row(html, "Purchases", submission.Details.Count.ToString(Australia));
        Row(html, "Submitted", submission.SubmittedAt?.ToLocalTime().ToString("d MMMM yyyy, h:mm tt", Australia));
        html.Append("</table>");

        html.Append(attach
            ? P("The attached PDF is the whole claim: the form, then every receipt in the order its manifest "
                + "lists them.")
            : P($"<strong>The PDF is too large to attach</strong> ({pdf.LongLength / 1024 / 1024:N0} MB). "
                + "Download it from the link below."));

        html.Append(P($"Download the PDF again at any time: <a href=\"{E(link)}\">{E(link)}</a>"));
        html.Append(P("Replying to this email replies to the claimant."));
        html.Append(Close());

        return new OutgoingMessage(
            To: config.AccountsAddress,
            ReplyTo: submission.SubmitterEmail,
            Subject: $"{(submission.IsMockData ? "[MOCK] " : "")}Expense claim {submission.Reference} — "
                     + $"{submission.SubmitterName} — {Money(submission.NetTotal)}",
            HtmlBody: html.ToString(),
            Attachment: attach ? new MailAttachment(ClaimPdf.FileName(submission), "application/pdf", pdf) : null);
    }

    /// <summary>
    /// To the claimant: it arrived, here is the reference. <c>Reply-To</c> is accounts, so a reply reaches a
    /// person rather than the sending mailbox.
    /// </summary>
    /// <remarks>
    /// No PDF and no link. It is a receipt, not a copy; the claim carries their name, contact details and
    /// card digits, and the less of that travelling to a typed-in address the better if they mistyped it.
    /// </remarks>
    public OutgoingMessage ReceiptToClaimant(DbExpenseSubmission submission)
    {
        string name = string.IsNullOrWhiteSpace(submission.SubmitterName) ? "there" : submission.SubmitterName;

        StringBuilder html = new();
        html.Append(Open());
        html.Append(P($"Hi {E(name)},"));
        html.Append(P($"We have received your {E(KindName(submission.Kind))}. Your claim reference is "
                      + $"<strong>{E(submission.Reference)}</strong>; please quote it if you contact us "
                      + "about this claim."));

        html.Append("<table cellpadding=\"4\" style=\"border-collapse:collapse\">");
        Row(html, NetLabel(submission.Kind), Money(submission.NetTotal));
        Row(html, "Purchases", submission.Details.Count.ToString(Australia));
        Row(html, "Submitted", submission.SubmittedAt?.ToLocalTime().ToString("d MMMM yyyy, h:mm tt", Australia));
        html.Append("</table>");

        html.Append(P("The accounts team will review it and be in touch if they need anything else. You can "
                      + "reply to this email to reach them."));
        html.Append(P("Good Shepherd Baptist Church — Finance"));
        html.Append(Close());

        return new OutgoingMessage(
            To: submission.SubmitterEmail
                ?? throw new InvalidOperationException("A submitted claim has no email address."),
            ReplyTo: config.AccountsAddress,
            Subject: $"{(submission.IsMockData ? "[MOCK] " : "")}We have received your expense claim "
                     + submission.Reference,
            HtmlBody: html.ToString(),
            Attachment: null);
    }

    private static string KindName(SubmissionKind kind) => kind == SubmissionKind.DebitCardPurchase
        ? "debit card purchase claim"
        : "expense reimbursement claim";

    /// <summary>What the net figure means on each form - authorised spend on one, money owed on the other.</summary>
    private static string NetLabel(SubmissionKind kind) => kind == SubmissionKind.DebitCardPurchase
        ? "Net church expense"
        : "Reimbursement claimed";

    private static void Row(StringBuilder html, string label, string? value) =>
        html.Append($"<tr><td style=\"color:#555\">{E(label)}</td><td><strong>{E(value ?? "—")}</strong></td></tr>");

    private static string P(string inner) => $"<p>{inner}</p>";

    private static string Open() =>
        "<div style=\"font-family:-apple-system,Segoe UI,Arial,sans-serif;font-size:14px;color:#111\">";

    private static string Close() => "</div>";

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    private static string Money(decimal value) => value.ToString("C2", Australia);
}

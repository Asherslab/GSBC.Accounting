using System.Net;

namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>
/// Writes each message to a folder instead of sending it: an <c>.html</c> file you can open in a browser,
/// with its attachment beside it.
/// </summary>
/// <remarks>
/// For local development, which has no Microsoft 365 credentials and must not email the real accounts
/// team every time somebody submits a mock claim. It exercises everything except Graph itself - the
/// outbox, the lease, the message the app builds and the combined PDF.
/// </remarks>
public class DirectoryMailTransport(MailConfig config, ILogger<DirectoryMailTransport> logger) : IMailTransport
{
    public async Task SendAsync(OutgoingMessage message, CancellationToken token)
    {
        Directory.CreateDirectory(config.DirectoryPath);

        string stem = Path.Combine(config.DirectoryPath,
            $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Safe(message.To)}-{Guid.NewGuid().ToString()[..8]}");

        // The envelope as a visible header block, since there is no mail client here to show it.
        string html =
            "<pre style=\"background:#eee;padding:8px\">"
            + WebUtility.HtmlEncode(
                $"To: {message.To}\nReply-To: {message.ReplyTo}\nSubject: {message.Subject}\n"
                + $"Attachment: {message.Attachment?.FileName ?? "(none)"}"
                + (message.Attachment is { } a ? $" ({a.Content.Length:N0} bytes)" : ""))
            + "</pre>"
            + message.HtmlBody;

        await File.WriteAllTextAsync(stem + ".html", html, token);

        if (message.Attachment is { } attachment)
            await File.WriteAllBytesAsync($"{stem}-{attachment.FileName}", attachment.Content, token);

        logger.LogInformation("Wrote email to {To} ({Subject}) to {Path}.html", message.To, message.Subject, stem);
    }

    private static string Safe(string value) =>
        new(value.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' ? c : '_').ToArray());
}

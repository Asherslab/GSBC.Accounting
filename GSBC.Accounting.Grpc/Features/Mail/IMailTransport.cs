namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>Hands one message to whatever actually delivers it. Throws if it was not accepted.</summary>
public interface IMailTransport
{
    Task SendAsync(OutgoingMessage message, CancellationToken token);
}

/// <summary>One email, as the app builds it. HTML only - every client either renders it or falls back.</summary>
public sealed record OutgoingMessage(
    string To,
    string? ReplyTo,
    string Subject,
    string HtmlBody,
    MailAttachment? Attachment
);

public sealed record MailAttachment(string FileName, string ContentType, byte[] Content);

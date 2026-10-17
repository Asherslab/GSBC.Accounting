using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;

namespace GSBC.Accounting.Grpc.Features.Mail;

/// <summary>
/// Sends as <see cref="MailConfig.Sender"/> through Microsoft Graph, with an app registration's client
/// credentials.
/// </summary>
/// <remarks>
/// <b>Graph rather than SMTP.</b> Exchange Online is retiring basic-auth SMTP submission during 2026, and
/// SMTP with OAuth needs the same app registration as this with an older protocol on top. Graph needs no
/// mailbox password, nobody signed in, and nothing that expires except the client secret.
/// <para>
/// <b>Two paths, chosen by attachment size, and both are load-bearing.</b> <c>sendMail</c> takes the whole
/// message in one request, but Graph refuses a request over 4 MB, which an inline attachment reaches at
/// about 3 MB of PDF - one phone photo. Above that the message is created as a draft, the PDF goes up
/// through an upload session in chunks, and the draft is sent. Both end with a copy in the sender's Sent
/// Items, which is the audit trail of what accounts were sent.
/// </para>
/// <para>
/// Plain <c>HttpClient</c> rather than the Graph SDK: four endpoints do not justify a dependency of that
/// size, and every request here is visible in one file.
/// </para>
/// </remarks>
public class GraphMailTransport(
    MailConfig config,
    HttpClient http,
    ILogger<GraphMailTransport> logger
) : IMailTransport
{
    /// <summary>Under Graph's 4 MB request ceiling once base64 and the message body are added.</summary>
    private const int InlineLimit = 3 * 1024 * 1024;

    /// <summary>Graph's ceiling per upload-session request. Must be a multiple of 320 KiB.</summary>
    private const int ChunkSize = 320 * 1024 * 12;

    private static readonly string[] Scopes = ["https://graph.microsoft.com/.default"];

    // One credential for the process: it caches the token and refreshes it before expiry, so building
    // one per send would fetch a fresh token for every email.
    private static ClientSecretCredential? _credential;

    public async Task SendAsync(OutgoingMessage message, CancellationToken token)
    {
        string user = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(config.Sender)}";

        if (message.Attachment is null || message.Attachment.Content.Length <= InlineLimit)
        {
            JsonObject body = new()
            {
                ["message"] = Message(message, includeAttachment: true),
                ["saveToSentItems"] = true
            };

            await SendJsonAsync(HttpMethod.Post, $"{user}/sendMail", body, token);

            return;
        }

        JsonElement draft = await SendJsonAsync(HttpMethod.Post, $"{user}/messages",
            Message(message, includeAttachment: false), token);

        string id = draft.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException("Graph created a draft with no id.");

        string messageUrl = $"{user}/messages/{Uri.EscapeDataString(id)}";

        try
        {
            await UploadAsync(messageUrl, message.Attachment, token);
            await SendJsonAsync(HttpMethod.Post, $"{messageUrl}/send", null, token);
        }
        catch
        {
            // Left behind, a half-built draft sits in the shared mailbox's Drafts looking like an email
            // somebody forgot to send. The retry builds a new one, so this one is only clutter.
            try
            {
                await SendJsonAsync(HttpMethod.Delete, messageUrl, null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete the abandoned Graph draft {MessageId}", id);
            }

            throw;
        }
    }

    private async Task UploadAsync(string messageUrl, MailAttachment attachment, CancellationToken token)
    {
        JsonObject request = new()
        {
            ["AttachmentItem"] = new JsonObject
            {
                ["attachmentType"] = "file",
                ["name"] = attachment.FileName,
                ["size"] = attachment.Content.Length,
                ["contentType"] = attachment.ContentType
            }
        };

        JsonElement session = await SendJsonAsync(HttpMethod.Post,
            $"{messageUrl}/attachments/createUploadSession", request, token);

        string uploadUrl = session.GetProperty("uploadUrl").GetString()
                           ?? throw new InvalidOperationException("Graph returned no upload URL.");

        int total = attachment.Content.Length;

        for (int start = 0; start < total; start += ChunkSize)
        {
            int length = Math.Min(ChunkSize, total - start);

            // NO Authorization header. The upload URL carries its own token, and Outlook's upload endpoint
            // answers 401 to a request that sends a bearer token as well.
            using HttpRequestMessage put = new(HttpMethod.Put, uploadUrl)
            {
                Content = new ByteArrayContent(attachment.Content, start, length)
            };
            put.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            put.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, start + length - 1, total);

            using HttpResponseMessage response = await http.SendAsync(put, token);
            await EnsureSuccessAsync(response, "upload a chunk of the attachment", token);
        }
    }

    private static JsonObject Message(OutgoingMessage message, bool includeAttachment)
    {
        JsonObject json = new()
        {
            ["subject"] = message.Subject,
            ["body"] = new JsonObject { ["contentType"] = "HTML", ["content"] = message.HtmlBody },
            ["toRecipients"] = new JsonArray(Recipient(message.To))
        };

        if (message.ReplyTo is { Length: > 0 } replyTo)
            json["replyTo"] = new JsonArray(Recipient(replyTo));

        if (includeAttachment && message.Attachment is { } attachment)
        {
            json["attachments"] = new JsonArray(new JsonObject
            {
                ["@odata.type"] = "#microsoft.graph.fileAttachment",
                ["name"] = attachment.FileName,
                ["contentType"] = attachment.ContentType,
                ["contentBytes"] = Convert.ToBase64String(attachment.Content)
            });
        }

        return json;
    }

    private static JsonObject Recipient(string address) =>
        new() { ["emailAddress"] = new JsonObject { ["address"] = address } };

    private async Task<JsonElement> SendJsonAsync(HttpMethod method, string url, JsonNode? body,
        CancellationToken token)
    {
        _credential ??= new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret);

        AccessToken access = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), token);

        using HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access.Token);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        using HttpResponseMessage response = await http.SendAsync(request, token);
        await EnsureSuccessAsync(response, $"{method} {new Uri(url).AbsolutePath}", token);

        // sendMail, send and delete answer with no body; only the create calls return something to read.
        string text = await response.Content.ReadAsStringAsync(token);

        return text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>
    /// Throws with Graph's own error text. Its error bodies say exactly what is wrong -
    /// <c>ErrorAccessDenied</c> for a mailbox the app is not scoped to, <c>MailboxNotEnabledForRESTAPI</c>
    /// for a sender that does not exist - and a bare status code would throw that away.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string what, CancellationToken token)
    {
        if (response.IsSuccessStatusCode)
            return;

        string detail = await response.Content.ReadAsStringAsync(token);

        if (detail.Length > 1000)
            detail = detail[..1000];

        throw new HttpRequestException(
            $"Graph refused to {what}: {(int)response.StatusCode} {response.ReasonPhrase}. {detail}",
            null, response.StatusCode);
    }
}

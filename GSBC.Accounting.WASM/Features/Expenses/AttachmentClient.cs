using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Uploads and lists receipts over plain HTTP, against a submission that already exists.
/// </summary>
/// <remarks>
/// Not gRPC. grpc-web has no client streaming, so a 20 MB receipt would have to be one buffered message
/// on both ends; the bytes go up a normal request body and YARP forwards <c>/api/</c> straight through.
/// <para>
/// <b>The practical ceiling is here, not on the server.</b> Blazor WebAssembly cannot stream a request
/// body - the browser materialises it - so the whole file sits in the WASM heap for the duration of the
/// upload regardless of what the server does. That limit arrives well before the server's.
/// </para>
/// </remarks>
public class AttachmentClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Matches <c>AttachmentStoreConfig.MaxBytesPerFile</c>. Duplicated deliberately: the server is the
    /// authority and refuses anything over it, but a browser that discovers the limit only after
    /// pushing 40 MB up a phone connection has wasted somebody's data allowance.
    /// </summary>
    public const long MaxBytes = 20L * 1024 * 1024;

    public async Task<AttachmentUploadResult> UploadAsync(
        Guid submissionId,
        string fileName,
        string contentType,
        Stream content,
        AttachmentKind kind,
        CancellationToken token = default
    )
    {
        string url = $"api/submissions/{submissionId}/attachments"
                     + $"?fileName={Uri.EscapeDataString(fileName)}&kind={kind}";

        StreamContent body = new(content);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        HttpResponseMessage response = await http.PostAsync(url, body, token);

        if (response.IsSuccessStatusCode)
        {
            ExpenseAttachment? attachment =
                await response.Content.ReadFromJsonAsync<ExpenseAttachment>(Json, token);

            return AttachmentUploadResult.Ok(attachment!);
        }

        // The server's refusals are written for the person holding the phone - "that file says it is
        // image/jpeg but its contents are application/pdf" - so they are surfaced as-is rather than
        // replaced with a generic failure.
        try
        {
            ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>(Json, token);

            if (!string.IsNullOrWhiteSpace(error?.Error))
                return AttachmentUploadResult.Failed(error.Error);
        }
        catch
        {
            // Not JSON - a proxy error page, or the connection died mid-response.
        }

        return AttachmentUploadResult.Failed($"That file could not be uploaded ({(int)response.StatusCode}).");
    }

    private record ApiError(string? Error);
}

public record AttachmentUploadResult(bool Success, ExpenseAttachment? Attachment, string? Error)
{
    public static AttachmentUploadResult Ok(ExpenseAttachment attachment) => new(true, attachment, null);

    public static AttachmentUploadResult Failed(string error) => new(false, null, error);
}

using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace GSBC.Accounting.WASM.Extensions;

/// <summary>
/// Makes every outgoing request carry the browser's cookies, so the <c>__gsbc_anon</c> session
/// reaches the server.
/// </summary>
/// <remarks>
/// <b>Stated rather than relied on.</b> A WASM <c>HttpClient</c> is the fetch API underneath, whose
/// default credentials mode is <c>same-origin</c> - which is already enough today, because everything
/// this app talks to is served through the same YARP origin that served the app. Setting it explicitly
/// costs nothing and makes the dependency visible: without cookies on these requests, a claimant's
/// drafts silently belong to nobody and every one of them comes back "could not be found".
/// <para>
/// <c>Include</c> rather than <c>SameOrigin</c> so the app survives the proxy moving to a host of its
/// own. That would need CORS with <c>Access-Control-Allow-Credentials</c> at the other end; it is not
/// the shape today, and this handler is not what would make it work - but it is one less thing to
/// rediscover if it ever is.
/// </para>
/// </remarks>
public class BrowserCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return base.SendAsync(request, cancellationToken);
    }
}

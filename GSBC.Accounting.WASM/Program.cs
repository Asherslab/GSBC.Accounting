using System.Globalization;
using GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;
using GSBC.Accounting.WASM;
using GSBC.Accounting.WASM.Extensions;
using GSBC.Accounting.WASM.Features.Expenses;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// PINNED, not inherited from the browser. A WASM app defaults to the viewer's locale, so ToString("C2")
// on a laptop set to en-GB renders the church's money as pounds - observed on 2026-08-31, "£0.00" in the
// action bar of the debit card form. This is an Australian church filing to the ACNC and the ATO: the
// currency is AUD and the date order is day-first regardless of who opens the page.
//
// Set before the host is built, because component code reads CurrentCulture as it renders.
CultureInfo australia = new("en-AU");
CultureInfo.DefaultThreadCurrentCulture = australia;
CultureInfo.DefaultThreadCurrentUICulture = australia;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Resolves the "https://yarp" addresses the gRPC clients are registered against. Comes from
// Aspire4Wasm.WebAssembly, not from the ServiceDefaults project - a WASM app cannot reference that.
builder.AddServiceDefaults();

// Same-origin, because everything this app talks to arrives through the YARP proxy that served it -
// which is also what lets the draft session cookie ride on these requests. BrowserCredentialsHandler
// states that dependency rather than leaving it to the fetch default.
builder.Services.AddScoped(_ => new HttpClient(new BrowserCredentialsHandler
{
    InnerHandler = new HttpClientHandler()
})
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// No authentication handler to add HERE, and there is still no token: the server authenticates the
// __gsbc_anon cookie itself as an anonymous session (docs/modules/expenses/drafts.md), and the browser
// attaches it. What that means for this client is that almost every call is gated - a browser that has
// never saved a draft gets Unauthenticated from everything except Create - so callers have to treat a
// refusal as "no drafts" or "unavailable" rather than as a server fault. A page injecting an unregistered service fails at RUNTIME, not at build, so a new service
// contract needs a line here as well as a MapGrpcService on the server.
builder.Services.AddCodeFirstClient<IExpenseSubmissionService>();

// In-progress forms live on the server, owned by an anonymous session cookie - see
// docs/modules/expenses/drafts.md. There is deliberately no localStorage copy any more: two places to
// look for the same draft is how a claimant ends up resuming the older one.
//
// Receipts go up a plain HTTP body, not the gRPC channel - see AttachmentClient. Same-origin, so it
// reuses the HttpClient registered above, and the cookie rides along with it.
builder.Services.AddScoped<AttachmentClient>();

await builder.Build().RunAsync();

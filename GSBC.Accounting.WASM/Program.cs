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

// Same-origin, because everything this app talks to arrives through the YARP proxy that served it.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// There is no authentication handler to add: both form pages are anonymous by design - see
// docs/work/2026-08-expense-forms-scope.md. A page injecting an unregistered service fails at RUNTIME,
// not at build, so a new service contract needs a line here as well as a MapGrpcService on the server.
builder.Services.AddCodeFirstClient<IExpenseSubmissionService>();

// In-progress forms live in the browser, never on the server - a draft belonging to nobody is either
// unrecoverable or enumerable by anyone with the URL, and a half-filled reimbursement form carries a
// claimant's contact details.
builder.Services.AddScoped<DraftStore>();

// Receipts go up a plain HTTP body, not the gRPC channel - see AttachmentClient. Same-origin, so it
// reuses the HttpClient registered above.
builder.Services.AddScoped<AttachmentClient>();

await builder.Build().RunAsync();

using GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;
using GSBC.Accounting.WASM;
using GSBC.Accounting.WASM.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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

await builder.Build().RunAsync();

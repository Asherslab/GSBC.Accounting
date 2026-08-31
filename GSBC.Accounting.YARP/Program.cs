using GSBC.Accounting.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The BFF layer, kept even though there is nothing to authenticate yet. Both form pages are
// anonymous by design (docs/work/2026-08-expense-forms-scope.md), but the proxy is where a sign-in
// would be hung when finance eventually wants an approval queue - and putting it here now costs
// nothing while retrofitting it would mean rearranging every client address.
//
// Deliberately absent, compared with GSBC.ImpactKids' proxy: no OpenID Connect, no cookie schemes, no
// authorization policies and no bearer-token transform. There is no identity provider, so a
// half-configured one would only fail in confusing ways.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();

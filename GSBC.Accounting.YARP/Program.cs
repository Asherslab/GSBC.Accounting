using GSBC.Accounting.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The proxy needs its OWN body ceiling, and it has to be at least as tight as the one behind it.
//
// Without this a 25 MB upload was answered 502. The gRPC service refuses an over-size body early, from
// its Content-Length, and closes the connection without draining it; YARP is still writing the rest and
// sees a broken pipe, which it reports as a bad gateway. The caller learns nothing, and the log points
// at the proxy rather than at the file.
//
// Measured on 2026-08-31: 22 MB (over the app's 20 MB limit, under this one) came back as a clean 413
// with the app's own message, while 25 MB came back 502. With this limit the 25 MB case is refused here
// instead, before anything is forwarded.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 24L * 1024 * 1024);

// The BFF layer. Nobody signs in, but the gRPC service now authenticates the __gsbc_anon cookie as an
// anonymous session (docs/modules/expenses/drafts.md) - so there IS something being authenticated,
// just not here. The proxy holds no signing key and no database connection, so it can neither mint that
// cookie nor check it; it forwards the Cookie header in and Set-Cookie back out, and the browser stores
// the result against this origin because everything - the WASM app, /gRPC/ and /api/ - is served from
// here. A sealed envelope the proxy cannot open, which is the shape ImpactKids' display auth uses.
//
// This is still where a real sign-in would be hung when finance wants an approval queue, and putting
// the layer here now costs nothing while retrofitting it would mean rearranging every client address.
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

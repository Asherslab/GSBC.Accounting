using GSBC.Accounting.Grpc.Data;
using Amazon.Runtime;
using Amazon.S3;
using GSBC.Accounting.Grpc.Extensions;
using GSBC.Accounting.Grpc.Features.Attachments;
using GSBC.Accounting.Grpc.Features.Pdf;
using GSBC.Accounting.Grpc.Features.Sessions;
using QuestPDF.Infrastructure;
using GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;
using GSBC.Accounting.ServiceDefaults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc.Server;

// QuestPDF is MIT-licensed for organisations under $1M annual revenue, which this church is. Declared
// once at startup; without it the first render throws.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCodeFirstGrpc();
builder.Services.AddGrpc();
builder.Services.AddConverters();

builder.Services.AddPooledDbContextFactory<AccountingDbContext>((_, o) =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("accounting"));
});

// The pooled factory gives out contexts; services inject AccountingDbContext directly, so bridge the
// two. Scoped, so one request gets one context and one SaveChanges.
builder.Services.AddScoped<AccountingDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AccountingDbContext>>().CreateDbContext());

// The receipt store. Absent configuration is NOT a legitimate state here, unlike GSBC.ImpactKids'
// photo store: a deployment with no photo store simply shows coloured initials, whereas a deployment
// with no attachment store cannot accept the itemised receipt that is the whole point of the form. So
// this registers unconditionally and fails loudly at startup if the configuration is incomplete.
AttachmentStoreConfig attachments =
    builder.Configuration.GetSection(AttachmentStoreConfig.SectionName).Get<AttachmentStoreConfig>()
    ?? new AttachmentStoreConfig();

builder.Services.AddSingleton(attachments);
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    new BasicAWSCredentials(attachments.AccessKey, attachments.SecretKey),
    new AmazonS3Config
    {
        ServiceURL = attachments.ServiceUrl,
        // Neither the local SeaweedFS nor the in-cluster one has per-bucket DNS, so the bucket has to
        // travel in the path rather than the hostname. Without this every request goes to a host that
        // does not resolve.
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1"
    }));
builder.Services.AddScoped<AttachmentStore>();

builder.Services.AddGsbcRateLimiting();

// Draft ownership. AnonymousSessions reads the __gsbc_anon cookie off the current request, so it needs
// the accessor - which is NOT registered by default in a gRPC-only host, and whose absence would show
// up as a null HttpContext and a session that could never be resolved rather than as a startup error.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AnonymousSessions>();

// The anonymous-session scheme, the AnonymousSession policy, and a deny-by-default FallbackPolicy.
// See Extensions/Authorization.cs - every endpoint that is genuinely open has to say so out loud.
builder.Services.AddGsbcAuthorization();

// Ninety days after its last edit, an abandoned draft is soft-deleted - it carries a claimant's name
// and contact details for a claim nobody will ever submit. Hosted here rather than in a worker of its
// own; every replica running it is harmless.
builder.Services.AddHostedService<DraftPurgeService>();

// Kestrel's own ceiling, below every application-level check. It is what stops a body being read at all
// rather than being read and then refused, and it applies to the gRPC endpoints too - where a 20 MB
// message would otherwise be buffered before anything looked at it.
//
// 24 MB rather than 20: the attachment endpoint's own limit is the real one, and leaving a margin means
// an over-size upload gets this app's readable "larger than 20 MB" message instead of Kestrel's abrupt
// connection reset.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 24L * 1024 * 1024);

var app = builder.Build();

// Before the rate limiter, so partitions are keyed on the caller's address rather than on YARP's.
// In the cluster this trusts the ingress to set the headers; nothing else may.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRateLimiter();

app.MapDefaultEndpoints();

// grpc-web, because the caller is Blazor WebAssembly in a browser and a browser cannot speak HTTP/2
// gRPC framing from fetch. DefaultEnabled = true is what lets it talk to this service at all.
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// AUTHENTICATION, but of a browser rather than of a person. Nobody signs in; the __gsbc_anon cookie
// is authenticated as an "anonymous session" (Features/Sessions/AnonymousSessionDefaults.cs) so that
// ownership is enforced by a policy and a deny-by-default fallback instead of by every method
// remembering to resolve it. What it proves is "this is the browser that saved that draft" - it
// identifies nobody, anybody holding the cookie is its owner, and no approval step, finance step or
// audit trail naming a person may ever be hung off it.
//
// Satisfying the policy is only the floor. It proves a session exists, never that the session owns the
// submission in the request, so the x.OwnerSessionId == sessionId predicate stays in every query.
//
// The pages are still reachable by strangers - Create has to be, or nobody could ever start - so the
// limits are still not optional: per-IP rate limits, an attachment count cap, a total-bytes cap and a
// content-type allow-list.
//
// After UseForwardedHeaders, because renewing the session re-sends the cookie and its Secure attribute
// follows the scheme the browser used. After UseRateLimiter, so a flood is refused before it costs a
// database lookup. Before the endpoints, which is what UseAuthorization requires.
app.UseAuthentication();
app.UseAuthorization();

// A service that compiles and is not mapped fails at the client as an unimplemented method, not at build.
app.MapGrpcService<ExpenseSubmissionService>()
    .RequireRateLimiting(RateLimiting.SubmissionPolicy)
    // CREATE IS EXEMPT HERE RATHER THAN BY AN ATTRIBUTE ON THE METHOD, and it has to be:
    // protobuf-net.Grpc does not carry method-level attributes onto the endpoint, so [AllowAnonymous]
    // on Create compiled, read correctly and did nothing - every method answered 401, including the
    // only one that can mint a session. See AllowAnonymousGrpcMethods.
    .AllowAnonymousGrpcMethods<ExpenseSubmissionService>("Create");

// Plain HTTP, never gRPC: a receipt is 1-20 MB and the gRPC channel must not carry file bytes.
app.AddAttachmentEndpoints();

// The printed form, rendered from the aggregate rather than from the HTML page - the screen layout and
// the printed layout are different problems and are allowed to diverge.
app.AddPdfEndpoints();

// A signpost for somebody who opened the address in a browser. Says nothing about this service's data.
// Anonymous explicitly, because of the FallbackPolicy. A signpost that answered 401 would be a
// confusing first impression of a service whose whole point is that nobody signs in.
app.MapGet("/",
        () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to "
              + "create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909")
    .AllowAnonymous();

// Best effort at startup. There is no ordering guarantee in the cluster, so the object store may not be
// up yet - and an unreachable store must not stop the service booting, because everything except
// attachments still works. PutAsync creates the bucket on demand if this did not manage it.
using (IServiceScope scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<AttachmentStore>().EnsureBucketAsync();
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogError(
            ex, "Could not reach the attachment store at startup. Uploads will fail until it is "
                + "reachable; everything else is unaffected");
    }
}

app.Run();

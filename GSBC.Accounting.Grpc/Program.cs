using GSBC.Accounting.Grpc.Data;
using Amazon.Runtime;
using Amazon.S3;
using GSBC.Accounting.Grpc.Extensions;
using GSBC.Accounting.Grpc.Features.Attachments;
using GSBC.Accounting.Grpc.Features.Pdf;
using QuestPDF.Infrastructure;
using GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;
using GSBC.Accounting.ServiceDefaults;
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

var app = builder.Build();

app.MapDefaultEndpoints();

// grpc-web, because the caller is Blazor WebAssembly in a browser and a browser cannot speak HTTP/2
// gRPC framing from fetch. DefaultEnabled = true is what lets it talk to this service at all.
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// There is no authentication here, and that is the design rather than a phase - see
// docs/work/2026-08-expense-forms-scope.md, "Anonymous is the design". Every submission is verified by
// a human before it goes anywhere. What that costs is that the submit and upload endpoints are open, so
// they carry their own limits: per-IP rate limits, an attachment count cap, a total-bytes cap and a
// content-type allow-list. Those land in slice 10 and are not optional.

// A service that compiles and is not mapped fails at the client as an unimplemented method, not at build.
app.MapGrpcService<ExpenseSubmissionService>();

// Plain HTTP, never gRPC: a receipt is 1-20 MB and the gRPC channel must not carry file bytes.
app.AddAttachmentEndpoints();

// The printed form, rendered from the aggregate rather than from the HTML page - the screen layout and
// the printed layout are different problems and are allowed to diverge.
app.AddPdfEndpoints();

// A signpost for somebody who opened the address in a browser. Says nothing about this service's data.
app.MapGet("/",
    () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to "
          + "create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

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

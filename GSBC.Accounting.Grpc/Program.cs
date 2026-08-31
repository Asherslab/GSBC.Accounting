using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCodeFirstGrpc();
builder.Services.AddGrpc();

builder.Services.AddPooledDbContextFactory<AccountingDbContext>((_, o) =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("accounting"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

// grpc-web, because the caller is Blazor WebAssembly in a browser and a browser cannot speak
// HTTP/2 gRPC framing from fetch.
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// There is no authentication here, and that is the design rather than a phase - see
// docs/work/2026-08-expense-forms-scope.md, "Anonymous is the design". Every submission is verified
// by a human before it goes anywhere. What that costs is that the submit and upload endpoints are
// open, so they carry their own limits: per-IP rate limits, an attachment count cap, a total-bytes
// cap and a content-type allow-list. Those land in slice 10 and are not optional.

// A signpost for somebody who opened the address in a browser. Says nothing about this service's data.
app.MapGet("/",
    () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to "
          + "create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

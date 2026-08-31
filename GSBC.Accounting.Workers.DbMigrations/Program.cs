using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.ServiceDefaults;
using GSBC.Accounting.Workers.DbMigrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<AccountingDbContext>("accounting");

var host = builder.Build();
host.Run();

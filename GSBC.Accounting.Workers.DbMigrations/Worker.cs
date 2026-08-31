using System.Diagnostics;
using GSBC.Accounting.Grpc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GSBC.Accounting.Workers.DbMigrations;

/// <summary>
/// Applies pending EF migrations, then stops the host. The gRPC service waits for this to run to
/// completion, so a schema change is always in place before anything reads or writes.
/// </summary>
/// <remarks>
/// There are no migrations yet - slice 1 adds the first one with <c>DbExpenseSubmission</c>. Until
/// then <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/> is a successful no-op, which is
/// the point: the wiring is real and proven before there is a schema to get wrong.
/// <para>
/// This worker never seeds. The two forms have no reference data - no chart of accounts, no expense
/// categories - because the finance destination is Xero and this app is the capture side only.
/// </para>
/// </remarks>
public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";

    private static readonly ActivitySource SActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using Activity? activity = SActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            AccountingDbContext dbContext = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();

            await RunMigrationAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(AccountingDbContext dbContext, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () => await dbContext.Database.MigrateAsync(cancellationToken));
    }
}

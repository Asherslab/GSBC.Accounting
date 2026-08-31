using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.Accounting.Grpc.Data;

/// <summary>
/// Lets <c>dotnet ef</c> construct the context without the Aspire host running.
/// </summary>
/// <remarks>
/// <b>The connection string comes from the environment, never from this file.</b> GSBC.ImpactKids
/// embeds its local Postgres password in the equivalent factory and that file is committed; it is only
/// a dev container's password, so the exposure is small, but there is no reason to repeat it.
/// <para>
/// <c>dotnet ef migrations add</c> only builds the model - it never opens a connection - so the
/// placeholder below is enough for the common case. Set the variable when running a command that does
/// connect (<c>database update</c>, <c>dbcontext script</c>):
/// </para>
/// <code>
/// c=$(docker ps -q --filter 'label=com.microsoft.developer.usvc-dev.mountsLabel=type=volume,src=gsbc-accounting-sql-data')
/// export ACCOUNTING_CONNECTION_STRING="Host=localhost;Port=60546;Database=accounting;Username=postgres;Password=$(docker exec $c printenv POSTGRES_PASSWORD)"
/// </code>
/// <para>
/// In normal running the migrations worker applies migrations and nothing uses this factory.
/// </para>
/// </remarks>
public class AccountingDbContextFactory : IDesignTimeDbContextFactory<AccountingDbContext>
{
    public const string ConnectionStringVariable = "ACCOUNTING_CONNECTION_STRING";

    public AccountingDbContext CreateDbContext(string[] args)
    {
        // Not a credential: a syntactically valid string so the model can be built offline. Any command
        // that actually connects with this will fail to authenticate, which is the intended outcome -
        // far better than a real password sitting in a tracked file.
        string connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? "Host=localhost;Port=60546;Database=accounting;Username=postgres;Password=design-time-only";

        DbContextOptionsBuilder<AccountingDbContext> options = new();
        options.UseNpgsql(connectionString);

        return new AccountingDbContext(options.Options);
    }
}

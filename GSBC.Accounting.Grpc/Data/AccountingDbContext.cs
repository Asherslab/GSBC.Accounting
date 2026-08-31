using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Data;

/// <summary>
/// The one context, split by subject area into <c>AccountingDbContext.&lt;Area&gt;Model.cs</c> files
/// that each hold their own <c>DbSet</c>s and a <c>Build&lt;Area&gt;Model</c>.
/// </summary>
/// <remarks>
/// Two rules apply to everything added here, both because this database holds financial records:
/// <list type="bullet">
/// <item><b>Money is <c>decimal(12,2)</c>, never <c>double</c>,</b> and the precision is configured
/// explicitly - see <c>BuildExpensesModel</c>.</item>
/// <item><b>Nothing hard-deletes.</b> ACNC retention is seven years; every entity carries
/// <c>Deleted</c> behind a global query filter.</item>
/// </list>
/// </remarks>
public partial class AccountingDbContext(DbContextOptions<AccountingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        BuildExpensesModel(modelBuilder);
    }
}

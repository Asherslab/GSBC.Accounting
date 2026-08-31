using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Data;

/// <summary>
/// The one context. EF models are <c>Db</c>-prefixed and live in <c>Data/Models/</c>; the contract
/// records the wire carries live in <c>GSBC.Accounting.Shared.Contracts</c> and are converted at the
/// service boundary.
/// </summary>
/// <remarks>
/// Two rules apply to everything added here, both because this database holds financial records:
/// <list type="bullet">
/// <item>
/// <b>Money is <c>decimal(12,2)</c>, never <c>double</c>.</b> Configure the precision explicitly in
/// <see cref="OnModelCreating"/> - Npgsql's default for an unconfigured <c>decimal</c> is
/// <c>numeric</c> with no precision at all, which stores what it is given and hides a client that
/// computed in floats.
/// </item>
/// <item>
/// <b>Nothing hard-deletes.</b> ACNC retention is seven years. Every entity carries <c>Deleted</c>
/// and every query filters <c>!x.Deleted</c> by hand, counts included.
/// </item>
/// </list>
/// There are no entities yet - slice 1 adds <c>DbExpenseSubmission</c> and <c>DbExpenseLine</c> along
/// with the first migration. Until then this exists so the migrations worker, the connection string
/// and the Aspire wiring are all real and proven.
/// </remarks>
public class AccountingDbContext(DbContextOptions<AccountingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

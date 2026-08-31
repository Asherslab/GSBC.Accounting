using GSBC.Accounting.Grpc.Data.Models.Expenses;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Data;

public partial class AccountingDbContext
{
    // Expression-bodied over `public required DbSet<T> { get; set; }`, which is what GSBC.ImpactKids
    // uses. `required` forces every construction site to initialise every DbSet to `null!` - including
    // the design-time factory, which then carries a growing list of them - and buys nothing, because
    // Set<T>() is what EF resolves either way.
    public DbSet<DbExpenseSubmission> ExpenseSubmissions => Set<DbExpenseSubmission>();

    public DbSet<DbExpenseLine> ExpenseLines => Set<DbExpenseLine>();

    private static void BuildExpensesModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbExpenseSubmission>()
            .HasMany(x => x.Lines)
            .WithOne(x => x.Submission)
            .HasForeignKey(x => x.SubmissionId)
            // Restrict, not Cascade. Nothing hard-deletes a submission, so a cascade would only ever
            // fire by accident - and if one did, it would take the evidence with it during a
            // seven-year retention window.
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete, applied once rather than trusted to every query. AGENTS.md asks for a hand-written
        // `!x.Deleted` in each read; a global filter is strictly safer for the same rule, because a
        // forgotten Where is the entire risk and a hard-deleted financial record is unrecoverable.
        // IgnoreQueryFilters() is how an audit view would ever see the deleted rows.
        modelBuilder.Entity<DbExpenseSubmission>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<DbExpenseLine>().HasQueryFilter(x => !x.Deleted);

        // Enums as strings. Readable in psql, and it survives a member being inserted in the middle of
        // the enum - which an int mapping does not, silently reinterpreting every existing row.
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.Kind).HasConversion<string>();
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.PaymentMethod).HasConversion<string>();
        modelBuilder.Entity<DbExpenseLine>().Property(x => x.Evidence).HasConversion<string>();

        // MONEY IS decimal(12,2), NEVER double, and the precision is not optional: an unconfigured
        // decimal maps to bare `numeric`, which happily stores whatever scale it is handed. That is how
        // a client that computed in JavaScript floats - as the mockup does - gets its rounding error
        // written to a financial record instead of being cut off at the column.
        foreach (string money in new[]
                 {
                     nameof(DbExpenseSubmission.AmountCharged),
                     nameof(DbExpenseSubmission.GrossTotal),
                     nameof(DbExpenseSubmission.GstTotal),
                     nameof(DbExpenseSubmission.LessPersonalAmount),
                     nameof(DbExpenseSubmission.NetTotal)
                 })
        {
            modelBuilder.Entity<DbExpenseSubmission>().Property(money).HasPrecision(12, 2);
        }

        modelBuilder.Entity<DbExpenseLine>().Property(x => x.GrossAmount).HasPrecision(12, 2);
        modelBuilder.Entity<DbExpenseLine>().Property(x => x.GstAmount).HasPrecision(12, 2);

        // A percentage, so 0.00 to 100.00 - three integer digits and two decimal places.
        modelBuilder.Entity<DbExpenseLine>().Property(x => x.ChurchUsePercent).HasPrecision(5, 2);

        // Four digits and nothing more, enforced at the column as well as in validation. The form says
        // "Never record the full card number"; a column that cannot hold one is the cheapest way to
        // keep that true.
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.CardLastFourDigits).HasMaxLength(4);

        // Every list of submissions is newest-first and filtered by kind.
        modelBuilder.Entity<DbExpenseSubmission>().HasIndex(x => new { x.Kind, x.CreatedAt });

        // Lines are always fetched for one submission, in table order.
        modelBuilder.Entity<DbExpenseLine>().HasIndex(x => new { x.SubmissionId, x.Ordinal });
    }
}

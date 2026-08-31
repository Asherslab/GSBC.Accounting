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

    public DbSet<DbExpenseAttachment> ExpenseAttachments => Set<DbExpenseAttachment>();

    public DbSet<DbExpenseAttendee> ExpenseAttendees => Set<DbExpenseAttendee>();

    public DbSet<DbExpenseTrip> ExpenseTrips => Set<DbExpenseTrip>();

    public DbSet<DbMissingReceiptDeclaration> MissingReceiptDeclarations => Set<DbMissingReceiptDeclaration>();

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
        modelBuilder.Entity<DbExpenseAttachment>().HasQueryFilter(x => !x.Deleted);

        // Enums as strings. Readable in psql, and it survives a member being inserted in the middle of
        // the enum - which an int mapping does not, silently reinterpreting every existing row.
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.Kind).HasConversion<string>();
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<DbExpenseSubmission>().Property(x => x.PaymentMethod).HasConversion<string>();
        modelBuilder.Entity<DbExpenseLine>().Property(x => x.Evidence).HasConversion<string>();
        modelBuilder.Entity<DbExpenseAttachment>().Property(x => x.Kind).HasConversion<string>();

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

        modelBuilder.Entity<DbExpenseSubmission>()
            .HasMany(x => x.Attachments)
            .WithOne(x => x.Submission)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // The same file uploaded twice to one submission is one object and one row.
        modelBuilder.Entity<DbExpenseAttachment>()
            .HasIndex(x => new { x.SubmissionId, x.ContentHash })
            .IsUnique();

        // SHA-256 as lowercase hex is exactly 64 characters, and nothing else belongs in this column.
        modelBuilder.Entity<DbExpenseAttachment>().Property(x => x.ContentHash).HasMaxLength(64);

        // A filename arrives from the claimant's device and is bounded here rather than trusted.
        modelBuilder.Entity<DbExpenseAttachment>().Property(x => x.FileName).HasMaxLength(260);
        modelBuilder.Entity<DbExpenseAttachment>().Property(x => x.ContentType).HasMaxLength(100);
        modelBuilder.Entity<DbExpenseAttachment>().Property(x => x.ObjectKey).HasMaxLength(400);

        // ---- Section 4 and 5 children ----

        modelBuilder.Entity<DbExpenseSubmission>()
            .HasMany(x => x.Attendees).WithOne(x => x.Submission)
            .HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DbExpenseSubmission>()
            .HasMany(x => x.Trips).WithOne(x => x.Submission)
            .HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);

        // 0..1: the paper form has one missing-receipt declaration or none.
        modelBuilder.Entity<DbExpenseSubmission>()
            .HasOne(x => x.MissingReceipt).WithOne(x => x.Submission)
            .HasForeignKey<DbMissingReceiptDeclaration>(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DbExpenseAttendee>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<DbExpenseTrip>().HasQueryFilter(x => !x.Deleted);
        modelBuilder.Entity<DbMissingReceiptDeclaration>().HasQueryFilter(x => !x.Deleted);

        modelBuilder.Entity<DbExpenseAttendee>().Property(x => x.Amount).HasPrecision(12, 2);
        modelBuilder.Entity<DbExpenseAttendee>().Property(x => x.PrivateShare).HasPrecision(12, 2);
        modelBuilder.Entity<DbMissingReceiptDeclaration>().Property(x => x.Amount).HasPrecision(12, 2);

        // Kilometres to one decimal, and a per-kilometre rate in cents - 0.880 needs three places, so
        // decimal(6,3) rather than the money precision used everywhere else.
        modelBuilder.Entity<DbExpenseTrip>().Property(x => x.BusinessKm).HasPrecision(8, 1);
        modelBuilder.Entity<DbExpenseTrip>().Property(x => x.ApprovedRate).HasPrecision(6, 3);

        modelBuilder.Entity<DbExpenseAttendee>().HasIndex(x => new { x.SubmissionId, x.Ordinal });
        modelBuilder.Entity<DbExpenseTrip>().HasIndex(x => new { x.SubmissionId, x.Ordinal });
    }
}

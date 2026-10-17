using GSBC.Accounting.Grpc.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace GSBC.Accounting.Grpc.Data;

public partial class AccountingDbContext
{
    public DbSet<DbOutgoingEmail> OutgoingEmails => Set<DbOutgoingEmail>();

    private static void BuildMailModel(ModelBuilder modelBuilder)
    {
        // A string, like every other enum here, so a row reads as what it is in psql.
        modelBuilder.Entity<DbOutgoingEmail>().Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);

        modelBuilder.Entity<DbOutgoingEmail>().Property(x => x.LastError).HasMaxLength(2000);

        // The sender's poll: unsent and due. Filtered, so the index stays the size of the backlog -
        // normally empty - rather than growing with every email ever sent.
        modelBuilder.Entity<DbOutgoingEmail>()
            .HasIndex(x => x.NextAttemptAt)
            .HasFilter("\"SentAt\" IS NULL");

        // One of each kind per claim. A retried Submit, or a bug that enqueued twice, is refused by the
        // database rather than emailing the accounts team the same claim twice.
        modelBuilder.Entity<DbOutgoingEmail>()
            .HasIndex(x => new { x.SubmissionId, x.Kind })
            .IsUnique();

        modelBuilder.Entity<DbOutgoingEmail>()
            .HasOne<Models.Expenses.DbExpenseSubmission>()
            .WithMany()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

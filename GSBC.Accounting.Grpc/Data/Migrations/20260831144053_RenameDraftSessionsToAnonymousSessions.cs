using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <summary>
    /// <c>DraftSessions</c> becomes <c>AnonymousSessions</c>, because the row is now the principal an
    /// authentication scheme resolves rather than a detail of the drafts list.
    /// </summary>
    /// <remarks>
    /// <b>Hand-written as a rename. EF scaffolded a DropTable + CreateTable and that was wrong.</b> It
    /// cannot infer a rename from a renamed DbSet, so it offered the destructive version - which would
    /// have left every existing submission's <c>OwnerSessionId</c> pointing at a row that no longer
    /// exists, silently, since it is a plain column and not a foreign key. Nothing in this app is in
    /// production yet, so the data loss would have been survivable; leaving a drop-and-create in the
    /// migration history for somebody to copy would not be.
    /// <para>
    /// Postgres renames neither indexes nor the primary-key constraint along with a table, so all three
    /// are renamed explicitly. Without that the table would carry indexes still named for
    /// <c>DraftSessions</c> and the next scaffolded migration would try to reconcile the difference.
    /// </para>
    /// <para>
    /// The cookie was renamed in the same change (<c>__gsbc_drafts</c> to <c>__gsbc_anon</c>), so the
    /// rows carried over here are no longer reachable by any browser - the sessions are dead even though
    /// the data survived, and the purge clears them when they expire. Preserving them is still the right
    /// migration: the alternative is a drop, and a drop is only safe by accident.
    /// </para>
    /// </remarks>
    public partial class RenameDraftSessionsToAnonymousSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DraftSessions",
                newName: "AnonymousSessions");

            migrationBuilder.RenameIndex(
                name: "IX_DraftSessions_ExpiresAt",
                newName: "IX_AnonymousSessions_ExpiresAt",
                table: "AnonymousSessions");

            migrationBuilder.RenameIndex(
                name: "IX_DraftSessions_TokenHash",
                newName: "IX_AnonymousSessions_TokenHash",
                table: "AnonymousSessions");

            // No MigrationBuilder verb for this one - the PK constraint keeps its old name through a
            // table rename, and a mismatched constraint name is the kind of drift that only shows up
            // three migrations later.
            migrationBuilder.Sql(
                """ALTER TABLE "AnonymousSessions" RENAME CONSTRAINT "PK_DraftSessions" TO "PK_AnonymousSessions";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """ALTER TABLE "AnonymousSessions" RENAME CONSTRAINT "PK_AnonymousSessions" TO "PK_DraftSessions";""");

            migrationBuilder.RenameIndex(
                name: "IX_AnonymousSessions_TokenHash",
                newName: "IX_DraftSessions_TokenHash",
                table: "AnonymousSessions");

            migrationBuilder.RenameIndex(
                name: "IX_AnonymousSessions_ExpiresAt",
                newName: "IX_DraftSessions_ExpiresAt",
                table: "AnonymousSessions");

            migrationBuilder.RenameTable(
                name: "AnonymousSessions",
                newName: "DraftSessions");
        }
    }
}

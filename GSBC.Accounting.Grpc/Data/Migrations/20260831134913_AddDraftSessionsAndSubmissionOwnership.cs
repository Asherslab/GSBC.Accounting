using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftSessionsAndSubmissionOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerSessionId",
                table: "ExpenseSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ExpenseSubmissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // HAND-WRITTEN, AND NOT OPTIONAL. A non-nullable DateTimeOffset column defaults to
            // 0001-01-01, which would leave every submission written before this migration looking
            // like it was last edited two thousand years ago - and DraftPurgeService soft-deletes any
            // draft whose UpdatedAt is more than ninety days old. Without this line the first purge
            // pass after deploying would throw away every existing draft in the database.
            //
            // CreatedAt is the honest value: before this column existed, creation was the only write
            // anything recorded.
            migrationBuilder.Sql("""UPDATE "ExpenseSubmissions" SET "UpdatedAt" = "CreatedAt";""");

            migrationBuilder.CreateTable(
                name: "DraftSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSubmissions_OwnerSessionId_UpdatedAt",
                table: "ExpenseSubmissions",
                columns: new[] { "OwnerSessionId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DraftSessions_ExpiresAt",
                table: "DraftSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DraftSessions_TokenHash",
                table: "DraftSessions",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DraftSessions");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseSubmissions_OwnerSessionId_UpdatedAt",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "OwnerSessionId",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ExpenseSubmissions");
        }
    }
}

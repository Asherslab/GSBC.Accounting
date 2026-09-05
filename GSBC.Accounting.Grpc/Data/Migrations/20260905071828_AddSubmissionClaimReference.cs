using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionClaimReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "ExpenseSubmissions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSubmissions_Reference",
                table: "ExpenseSubmissions",
                column: "Reference",
                unique: true,
                filter: "\"Reference\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseSubmissions_Reference",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "ExpenseSubmissions");
        }
    }
}

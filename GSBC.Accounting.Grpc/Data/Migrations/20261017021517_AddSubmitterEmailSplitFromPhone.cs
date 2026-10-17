using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmitterEmailSplitFromPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactPhoneEmail",
                table: "ExpenseSubmissions",
                newName: "ContactPhone");

            migrationBuilder.AddColumn<string>(
                name: "SubmitterEmail",
                table: "ExpenseSubmissions",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmitterEmail",
                table: "ExpenseSubmissions");

            migrationBuilder.RenameColumn(
                name: "ContactPhone",
                table: "ExpenseSubmissions",
                newName: "ContactPhoneEmail");
        }
    }
}

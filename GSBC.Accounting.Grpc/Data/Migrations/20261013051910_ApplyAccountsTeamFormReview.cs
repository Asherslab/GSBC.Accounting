using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApplyAccountsTeamFormReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseTrips");

            migrationBuilder.DropColumn(
                name: "AmountCharged",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "BankReference",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "EventProject",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ExpensePeriodFrom",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ExpensePeriodTo",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "PurposeActivity",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "PurposeNarrative",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "RoleOther",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "SupplierMerchant",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "TransactionTime",
                table: "ExpenseSubmissions");

            migrationBuilder.AddColumn<bool>(
                name: "PriorApprovalGiven",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorApprovalGiven",
                table: "ExpenseSubmissions");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountCharged",
                table: "ExpenseSubmissions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovalDate",
                table: "ExpenseSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankReference",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventProject",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpensePeriodFrom",
                table: "ExpenseSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpensePeriodTo",
                table: "ExpenseSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurposeActivity",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurposeNarrative",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleOther",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierMerchant",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TransactionDate",
                table: "ExpenseSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionTime",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseTrips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedRate = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    BusinessKm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    From = table.Column<string>(type: "text", nullable: true),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    To = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseTrips_ExpenseSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExpenseSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTrips_SubmissionId_Ordinal",
                table: "ExpenseTrips",
                columns: new[] { "SubmissionId", "Ordinal" });
        }
    }
}

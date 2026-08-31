using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceDeclarationsAndSection4Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComplianceDetails",
                table: "ExpenseSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComplianceQ1",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComplianceQ2",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComplianceQ3",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComplianceQ4",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComplianceQ5",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComplianceQ6",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Declaration1",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Declaration2",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Declaration3",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Declaration4",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Declaration5",
                table: "ExpenseSubmissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseAttendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Person = table.Column<string>(type: "text", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PrivateShare = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseAttendees_ExpenseSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExpenseSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseTrips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    From = table.Column<string>(type: "text", nullable: true),
                    To = table.Column<string>(type: "text", nullable: true),
                    BusinessKm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    ApprovedRate = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "MissingReceiptDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Supplier = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Declared = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissingReceiptDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissingReceiptDeclarations_ExpenseSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExpenseSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAttendees_SubmissionId_Ordinal",
                table: "ExpenseAttendees",
                columns: new[] { "SubmissionId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTrips_SubmissionId_Ordinal",
                table: "ExpenseTrips",
                columns: new[] { "SubmissionId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_MissingReceiptDeclarations_SubmissionId",
                table: "MissingReceiptDeclarations",
                column: "SubmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseAttendees");

            migrationBuilder.DropTable(
                name: "ExpenseTrips");

            migrationBuilder.DropTable(
                name: "MissingReceiptDeclarations");

            migrationBuilder.DropColumn(
                name: "ComplianceDetails",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ComplianceQ1",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ComplianceQ2",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ComplianceQ3",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ComplianceQ4",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ComplianceQ5",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "ComplianceQ6",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Declaration1",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Declaration2",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Declaration3",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Declaration4",
                table: "ExpenseSubmissions");

            migrationBuilder.DropColumn(
                name: "Declaration5",
                table: "ExpenseSubmissions");
        }
    }
}

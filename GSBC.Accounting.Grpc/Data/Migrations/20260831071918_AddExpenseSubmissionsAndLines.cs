using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseSubmissionsAndLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmitterName = table.Column<string>(type: "text", nullable: true),
                    FormDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: true),
                    RoleOther = table.Column<string>(type: "text", nullable: true),
                    MinistryDepartment = table.Column<string>(type: "text", nullable: true),
                    CardLastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    TransactionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TransactionTime = table.Column<string>(type: "text", nullable: true),
                    SupplierMerchant = table.Column<string>(type: "text", nullable: true),
                    AmountCharged = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    BankReference = table.Column<string>(type: "text", nullable: true),
                    ContactPhoneEmail = table.Column<string>(type: "text", nullable: true),
                    ExpensePeriodFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpensePeriodTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    PaymentMethodOther = table.Column<string>(type: "text", nullable: true),
                    BankDetailsOnFile = table.Column<bool>(type: "boolean", nullable: true),
                    PurposeActivity = table.Column<string>(type: "text", nullable: true),
                    EventProject = table.Column<string>(type: "text", nullable: true),
                    PriorApprovalBy = table.Column<string>(type: "text", nullable: true),
                    ApprovalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PurposeNarrative = table.Column<string>(type: "text", nullable: true),
                    GrossTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    GstTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LessPersonalAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    SignatureName = table.Column<string>(type: "text", nullable: true),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsMockData = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ItemDescription = table.Column<string>(type: "text", nullable: true),
                    LineDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    GstAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ChurchUsePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseLines_ExpenseSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExpenseSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseLines_SubmissionId_Ordinal",
                table: "ExpenseLines",
                columns: new[] { "SubmissionId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSubmissions_Kind_CreatedAt",
                table: "ExpenseSubmissions",
                columns: new[] { "Kind", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseLines");

            migrationBuilder.DropTable(
                name: "ExpenseSubmissions");
        }
    }
}

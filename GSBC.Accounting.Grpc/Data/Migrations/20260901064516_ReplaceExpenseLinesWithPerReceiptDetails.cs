using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.Accounting.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceExpenseLinesWithPerReceiptDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseLines");

            migrationBuilder.RenameColumn(
                name: "LineId",
                table: "ExpenseAttachments",
                newName: "DetailKey");

            migrationBuilder.CreateTable(
                name: "ExpenseDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Supplier = table.Column<string>(type: "text", nullable: true),
                    PurchaseDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    ContainsPersonalItems = table.Column<bool>(type: "boolean", nullable: true),
                    ReceiptIsItemised = table.Column<bool>(type: "boolean", nullable: true),
                    TotalIncGst = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    GstAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    NonReimbursedAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseDetails_ExpenseSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ExpenseSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseDetailItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsChurchUse = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDetailItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseDetailItems_ExpenseDetails_DetailId",
                        column: x => x.DetailId,
                        principalTable: "ExpenseDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAttachments_SubmissionId_DetailKey",
                table: "ExpenseAttachments",
                columns: new[] { "SubmissionId", "DetailKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDetailItems_DetailId_Ordinal",
                table: "ExpenseDetailItems",
                columns: new[] { "DetailId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDetails_SubmissionId_Key",
                table: "ExpenseDetails",
                columns: new[] { "SubmissionId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDetails_SubmissionId_Ordinal",
                table: "ExpenseDetails",
                columns: new[] { "SubmissionId", "Ordinal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseDetailItems");

            migrationBuilder.DropTable(
                name: "ExpenseDetails");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseAttachments_SubmissionId_DetailKey",
                table: "ExpenseAttachments");

            migrationBuilder.RenameColumn(
                name: "DetailKey",
                table: "ExpenseAttachments",
                newName: "LineId");

            migrationBuilder.CreateTable(
                name: "ExpenseLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChurchUsePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    GstAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ItemDescription = table.Column<string>(type: "text", nullable: true),
                    LineDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true)
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
        }
    }
}

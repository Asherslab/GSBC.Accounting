using GSBC.Accounting.Grpc.Conversion;
using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;

namespace GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;

/// <summary>
/// One file per operation, sharing this primary constructor. Mapped in <c>Program.cs</c> - a service
/// that compiles and is not mapped fails at the client as an unimplemented method, not at build.
/// </summary>
public partial class ExpenseSubmissionService(
    AccountingDbContext db,
    IConverter<DbExpenseSubmission, ExpenseSubmission> converter
) : IExpenseSubmissionService;

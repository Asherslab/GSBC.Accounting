using GSBC.Accounting.Grpc.Conversion;
using GSBC.Accounting.Grpc.Data;
using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Grpc.Features.Sessions;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;
using Microsoft.AspNetCore.Authorization;

namespace GSBC.Accounting.Grpc.Features.Expenses.ExpenseSubmissionServices;

/// <summary>
/// One file per operation, sharing this primary constructor. Mapped in <c>Program.cs</c> - a service
/// that compiles and is not mapped fails at the client as an unimplemented method, not at build.
/// </summary>
/// <remarks>
/// <b>Every method except <c>Create</c> resolves the caller's session first and filters on it.</b> The
/// pattern is the same everywhere: <c>sessions.CurrentAsync()</c>, then a query that includes
/// <c>x.OwnerSessionId == session</c> in its predicate rather than checking ownership after the fetch.
/// Filtering in the query is what makes "not yours" and "does not exist" the same answer, which is the
/// answer a caller should get - anything else is a way to ask the server which submission ids are real.
/// <para>
/// <b>The policy is the floor, the predicate is the check.</b> <see cref="Policies.AnonymousSession"/>
/// proves the caller holds a session at all - it cannot know whether that session owns the submission
/// in the request, so it replaces none of the predicates below it. What it does replace is the chance
/// of a new method forgetting them entirely and falling back to treating an id as authority.
/// </para>
/// <para>
/// <c>Create</c> is the only method that opts out, because it is the only minter. See
/// <c>Create.cs</c>.
/// </para>
/// </remarks>
[Authorize(Policy = Policies.AnonymousSession)]
public partial class ExpenseSubmissionService(
    AccountingDbContext db,
    AnonymousSessions sessions,
    IConverter<DbExpenseSubmission, ExpenseSubmission> converter
) : IExpenseSubmissionService;

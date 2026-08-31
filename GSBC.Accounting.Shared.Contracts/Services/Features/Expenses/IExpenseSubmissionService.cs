using GSBC.Accounting.Shared.Contracts.Messages.Requests.Features.Expenses;
using GSBC.Accounting.Shared.Contracts.Services.Base;

namespace GSBC.Accounting.Shared.Contracts.Services.Features.Expenses;

/// <summary>
/// The one service both form pages call.
/// </summary>
/// <remarks>
/// <b>The [Service] string is the wire path and must match the YARP route pattern</b>
/// (<c>/gRPC/GSBC.Accounting.{service}/{**catch-all}</c> in GSBC.Accounting.YARP/appsettings.json). A
/// mismatch is not a build error - the call falls through to the WASM catch-all route, comes back as
/// index.html with a 200, and grpc-web reports "Bad gRPC response. Invalid content-type value:
/// text/html".
/// <para>
/// There is no read, no list and no delete. Nothing in this scope reads a submission back: it is
/// submit-only, with no approval queue and no finance screen. The PDF render is a plain HTTP endpoint
/// rather than a method here, because a rendered document is not a gRPC message.
/// </para>
/// </remarks>
[Service("gRPC/GSBC.Accounting.ExpenseSubmissions")]
public interface IExpenseSubmissionService
    : ICreateService<CreateExpenseSubmissionRequest>;

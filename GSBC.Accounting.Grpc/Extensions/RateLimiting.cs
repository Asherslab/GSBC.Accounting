using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GSBC.Accounting.Grpc.Extensions;

/// <summary>
/// Per-IP ceilings on the endpoints a stranger can reach, which here is all of them.
/// </summary>
/// <remarks>
/// <b>This is the price of the pages being anonymous, and it is not optional.</b> There is no sign-in,
/// so nothing else stands between the open internet and the object store: without a limit, "create a
/// draft, upload, repeat" is an anonymous file host running on the church's storage bill.
/// <para>
/// The numbers are set for what a person filling in a form actually does, with a wide margin. A
/// claimant submits one or two forms and attaches a handful of receipts; anything an order of magnitude
/// past that is not somebody filling in a form.
/// </para>
/// <para>
/// <b>Partitioned by remote IP, which is the weakest part.</b> Behind YARP the remote address is the
/// proxy's unless forwarded headers are honoured, so <c>UseForwardedHeaders</c> runs first - and in the
/// cluster the ingress has to be trusted to set them. A shared NAT also puts a whole office in one
/// bucket. It is a speed bump against casual abuse rather than a defence against a determined attacker,
/// and it is worth being honest that real protection needs either authentication or something in front.
/// </para>
/// </remarks>
public static class RateLimiting
{
    /// <summary>Uploads: bytes in, so the tightest of the three.</summary>
    public const string UploadPolicy = "uploads";

    /// <summary>Create, update and submit. A form is saved repeatedly while it is being filled in.</summary>
    public const string SubmissionPolicy = "submissions";

    /// <summary>PDF renders, which cost server CPU rather than storage.</summary>
    public const string RenderPolicy = "renders";

    public static IServiceCollection AddGsbcRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 429, not the default 503. A client that is being throttled needs to be told that, and
            // 503 reads as "the server is broken" to anybody reading a log.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "That is more requests than this form needs. Wait a minute and try again."
                }, token);
            };

            // 30 uploads a minute per address. A claimant attaching a dozen receipts finishes well
            // inside it; a script filling the bucket does not.
            options.AddPolicy(UploadPolicy, PerAddress(permitLimit: 30));

            // Higher, because the page saves the draft on every submit attempt and on every edit that
            // follows one.
            options.AddPolicy(SubmissionPolicy, PerAddress(permitLimit: 120));

            options.AddPolicy(RenderPolicy, PerAddress(permitLimit: 20));
        });

        return services;
    }

    /// <summary>
    /// A fixed window per remote address.
    /// </summary>
    /// <remarks>
    /// Fixed rather than sliding: a sliding window keeps per-partition segment state, and this is an
    /// endpoint anybody can create partitions on simply by having an address. A fixed window's burst at
    /// the boundary is a fair trade for bounded memory here.
    /// <para>
    /// Requests with no remote address - which happens in some proxy configurations - share one
    /// partition rather than bypassing the limit entirely.
    /// </para>
    /// </remarks>
    private static Func<HttpContext, RateLimitPartition<string>> PerAddress(int permitLimit) =>
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                // No queue. Holding a request that is over the limit ties up a connection and delays the
                // answer the caller needs, which is "you are going too fast".
                QueueLimit = 0
            });
}

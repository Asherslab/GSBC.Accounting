using Grpc.Net.Client.Web;
using ProtoBuf.Grpc.ClientFactory;

namespace GSBC.Accounting.WASM.Extensions;

public static class GrpcServiceExtensions
{
    /// <summary>
    /// Registers a code-first gRPC client pointed at the YARP proxy.
    /// </summary>
    /// <remarks>
    /// <c>https://yarp</c> is an Aspire service-discovery name, resolved by
    /// <c>Aspire4Wasm.WebAssembly</c> plus <c>builder.AddServiceDefaults()</c>. The browser never talks
    /// to the gRPC service directly - everything goes through the BFF, which is what leaves room for a
    /// sign-in to be added there later without changing a single client address.
    /// <para>
    /// <see cref="GrpcWebHandler"/> is not optional: a browser cannot speak HTTP/2 gRPC framing.
    /// </para>
    /// <para>
    /// Not named "authenticated", unlike GSBC.ImpactKids' equivalent, and there is still no token to
    /// attach: nobody signs in. What these calls do carry is the <c>__gsbc_anon</c> cookie - see
    /// <see cref="BrowserCredentialsHandler"/> - which the server authenticates as an anonymous session
    /// and which decides whose drafts it will talk about. That authenticates a browser, not a person,
    /// and nothing may be hung off it that needs to know who somebody is.
    /// <para>
    /// <b>The cookie is load-bearing for almost every call.</b> Everything except <c>Create</c> is
    /// behind the <c>AnonymousSession</c> policy, so a browser that has never saved a draft is answered
    /// <c>Unauthenticated</c> rather than with an empty result - which callers have to handle as "none"
    /// or "unavailable", not as a server fault. If <see cref="BrowserCredentialsHandler"/> ever stops
    /// attaching cookies, the symptom is every call failing for a first-time visitor.
    /// </para>
    /// </para>
    /// </remarks>
    // Named AddCodeFirstClient, not AddGrpcClient: Grpc.Net.ClientFactory already ships an
    // AddGrpcClient<T> extension on IServiceCollection, and a same-named one here is an ambiguous-call
    // error at every call site rather than an override.
    public static IServiceCollection AddCodeFirstClient<T>(this IServiceCollection services) where T : class
    {
        services
            .AddCodeFirstGrpcClient<T>(typeof(T).FullName!, x => { x.Address = new Uri("https://yarp"); })
            .ConfigureChannel(x => { x.UnsafeUseInsecureChannelCallCredentials = true; })
            // The credentials handler sits INSIDE GrpcWebHandler, not around it. GrpcWebHandler is the
            // primary handler for this client and rewrites the request into grpc-web framing; a
            // DelegatingHandler wrapped outside it would be handed a request that has already been
            // built, and the browser request it produced would never see the flag.
            .ConfigurePrimaryHttpMessageHandler(() =>
                new GrpcWebHandler(new BrowserCredentialsHandler { InnerHandler = new HttpClientHandler() }));

        return services;
    }
}

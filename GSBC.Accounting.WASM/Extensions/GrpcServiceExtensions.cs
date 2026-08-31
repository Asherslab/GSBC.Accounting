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
    /// Not named "authenticated", unlike GSBC.ImpactKids' equivalent. There is no token to attach: both
    /// form pages are anonymous by design.
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
            .ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(new HttpClientHandler()));

        return services;
    }
}

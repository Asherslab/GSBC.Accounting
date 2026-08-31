using System.Reflection;
using GSBC.Accounting.Grpc.Conversion;

namespace GSBC.Accounting.Grpc.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Registers every <see cref="IConverter"/> in the loaded assemblies against each interface it
    /// implements, so adding a converter needs no wiring.
    /// </summary>
    public static IServiceCollection AddConverters(this IServiceCollection services)
    {
        List<Type> converters = [];

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            converters.AddRange(
                assembly.GetTypes()
                    .Where(x => x.IsAssignableTo(typeof(IConverter)) && x is { IsClass: true, IsAbstract: false })
            );
        }

        foreach (Type converter in converters)
        {
            foreach (Type interfaceType in converter.GetInterfaces())
            {
                services.AddScoped(interfaceType, converter);
            }
        }

        return services;
    }
}

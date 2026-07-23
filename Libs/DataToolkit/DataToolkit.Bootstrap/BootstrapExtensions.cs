using System.Reflection;
using DataToolkit.Bootstrap.Discovery;

namespace Microsoft.Extensions.DependencyInjection;

public static class BootstrapExtensions
{
    public static IServiceCollection AddBootstrap(
        this IServiceCollection services,
        bool Verbose = false,
        params (Assembly Assembly, string RootNamespace, string TargetNamespace)[] modules)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(modules);

        BootstrapModule[] bootstrapModules = new BootstrapModule[modules.Length];

        for (int i = 0; i < modules.Length; i++)
        {
            var module = modules[i];

            bootstrapModules[i] = new BootstrapModule(
                module.Assembly,
                module.RootNamespace,
                module.TargetNamespace);
        }

        var types = TypeScanner.Scan(bootstrapModules);

        BootstrapRegistrar.Register(services, Verbose, types);

        return services;
    }
}
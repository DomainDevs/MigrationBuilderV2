using DataToolkit.Bootstrap.Diagnostics;
using DataToolkit.Bootstrap.Discovery;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class BootstrapExtensions
{
    [Conditional("DEBUG")]
    public static void Dump(this IServiceCollection services)
    {
        foreach (var group in services
            .GroupBy(s => s.ServiceType.Assembly.GetName().Name)
            .OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"{group.Key,-45} {group.Count()}");
        }
    }

    public static IServiceCollection AddBootstrap(
        this IServiceCollection services,
        bool verbose = false,
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

        BootstrapProfiler profiler = new();

        IReadOnlyCollection<CandidateType> types =
            TypeScanner.Scan(bootstrapModules, profiler, out int excluded);

        BootstrapRegistrar.Register(
            services,
            verbose,
            types,
            profiler,
            excluded);
        //Console.WriteLine($"Bootstrap completed. Service collection: {services.Count} Services.");

        return services;
    }
}
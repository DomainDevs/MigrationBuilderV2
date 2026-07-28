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
        ArgumentNullException.ThrowIfNull(services);
        Dictionary<string, int> assemblies = new(StringComparer.Ordinal);

        foreach (ServiceDescriptor service in services)
        {
            string assembly = service.ServiceType.Assembly.GetName().Name ?? "<Unknown>";

            if (assemblies.TryGetValue(assembly, out int count))
            {
                assemblies[assembly] = count + 1;
            }
            else
            {
                assemblies.Add(assembly, 1);
            }
        }

        List<KeyValuePair<string, int>> ordered = new(assemblies);
        ordered.Sort(static (x, y) =>
        {
            int compare = y.Value.CompareTo(x.Value);
            return compare != 0
                ? compare
                : StringComparer.Ordinal.Compare(x.Key, y.Key);
        });

        foreach (var (assembly, count) in ordered)
        {
            Console.WriteLine($"{assembly,-45} {count}");
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

        return services;
    }
}
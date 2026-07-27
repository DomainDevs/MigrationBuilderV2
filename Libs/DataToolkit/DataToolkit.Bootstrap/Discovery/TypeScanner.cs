using System.Reflection;
using DataToolkit.Bootstrap.Diagnostics;
using DataToolkit.Bootstrap.Exceptions;

namespace DataToolkit.Bootstrap.Discovery;

internal static class TypeScanner
{
    internal static IReadOnlyCollection<CandidateType> Scan(
        IEnumerable<BootstrapModule> modules,
        BootstrapProfiler profiler,
        out int excluded)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(profiler);

        List<CandidateType> result = new(64);
        excluded = 0;

        foreach (BootstrapModule module in modules)
        {
            string rootNamespace = module.RootNamespace;
            string rootNamespacePrefix = module.RootNamespacePrefix;
            string targetNamespace = module.TargetNamespace;

            int matches = 0;

            profiler.Start(BootstrapPhase.AssemblyScan);

            Type[] types = GetLoadableTypes(module.Assembly);

            profiler.Stop();

            profiler.Start(BootstrapPhase.Reflection);

            foreach (Type type in types)
            {
                if (!IsCandidate(type))
                {
                    continue;
                }

                if (!MatchesNamespace(
                        type,
                        rootNamespace,
                        rootNamespacePrefix,
                        targetNamespace))
                {
                    continue;
                }

                ServiceRegistration registration =
                    RegistrationReader.Read(type);

                if (registration.Exclude)
                {
                    excluded++;
                    continue;
                }

                result.Add(new CandidateType(
                    type,
                    GetPublicInterfaces(type),
                    registration));

                matches++;
            }

            profiler.Stop();

            if (matches == 0)
            {
                throw new BootstrapConfigurationException(
                    $"No se encontró ningún tipo público registrable en el módulo '{rootNamespace}' " +
                    $"con TargetNamespace '{targetNamespace}' dentro del ensamblado '{module.Assembly.GetName().Name}'.");
            }
        }

        return result;
    }

    private static Type[] GetPublicInterfaces(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        switch (interfaces.Length)
        {
            case 0:
                return interfaces;

            case 1:
                return interfaces[0].IsPublic
                    ? interfaces
                    : [];
        }

        int count = 0;

        for (int i = 0; i < interfaces.Length; i++)
        {
            if (interfaces[i].IsPublic)
            {
                count++;
            }
        }

        if (count == interfaces.Length)
        {
            return interfaces;
        }

        Type[] result = new Type[count];

        int index = 0;

        for (int i = 0; i < interfaces.Length; i++)
        {
            Type current = interfaces[i];

            if (current.IsPublic)
            {
                result[index++] = current;
            }
        }

        return result;
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Type?[] source = ex.Types;

            int count = 0;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] is not null)
                {
                    count++;
                }
            }

            Type[] result = new Type[count];

            int index = 0;

            for (int i = 0; i < source.Length; i++)
            {
                Type? type = source[i];

                if (type is not null)
                {
                    result[index++] = type;
                }
            }

            return result;
        }
    }

    private static bool IsCandidate(Type type)
    {
        return
            type.IsPublic &&
            type.IsClass &&
            !type.IsAbstract &&
            !type.IsNested &&
            !type.IsGenericTypeDefinition;
    }

    private static bool MatchesNamespace(
        Type type,
        string rootNamespace,
        string rootNamespacePrefix,
        string targetNamespace)
    {
        string? ns = type.Namespace;

        if (ns is null)
        {
            return false;
        }

        if (ns != rootNamespace &&
            !ns.StartsWith(rootNamespacePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> span = ns.AsSpan();

        int separator = span.LastIndexOf('.');

        ReadOnlySpan<char> lastSegment =
            separator >= 0
                ? span[(separator + 1)..]
                : span;

        return lastSegment.Equals(
            targetNamespace,
            StringComparison.Ordinal);
    }
}
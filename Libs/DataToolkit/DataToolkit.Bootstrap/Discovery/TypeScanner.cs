using System.Reflection;

namespace DataToolkit.Bootstrap.Discovery;

internal static class TypeScanner
{
    internal static IReadOnlyCollection<CandidateType> Scan(
        IEnumerable<BootstrapModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        List<CandidateType> result = new(64);

        foreach (BootstrapModule module in modules)
        {
            foreach (Type type in GetLoadableTypes(module.Assembly))
            {
                if (!IsCandidate(type))
                {
                    continue;
                }

                if (!MatchesNamespace(type, module))
                {
                    continue;
                }

                ServiceRegistration registration =
                    RegistrationReader.Read(type);

                if (registration.Exclude)
                {
                    continue;
                }

                result.Add(new CandidateType(
                    type,
                    GetPublicInterfaces(type),
                    registration));
            }
        }

        return result;
    }

    private static Type[] GetPublicInterfaces(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        if (interfaces.Length == 0)
        {
            return interfaces;
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
            if (interfaces[i].IsPublic)
            {
                result[index++] = interfaces[i];
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
        return type.IsClass
            && !type.IsAbstract
            && type.IsPublic
            && !type.IsNested
            && !type.IsGenericTypeDefinition;
    }

    private static bool MatchesNamespace(
        Type type,
        BootstrapModule module)
    {
        string? ns = type.Namespace;

        if (ns is null)
        {
            return false;
        }

        string rootNamespace = module.RootNamespace;

        if (ns != rootNamespace &&
            !ns.StartsWith(module.RootNamespacePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> span = ns.AsSpan();

        int lastSeparator = span.LastIndexOf('.');

        ReadOnlySpan<char> lastSegment =
            lastSeparator >= 0
                ? span[(lastSeparator + 1)..]
                : span;

        return lastSegment.Equals(
            module.TargetNamespace,
            StringComparison.Ordinal);
    }
}
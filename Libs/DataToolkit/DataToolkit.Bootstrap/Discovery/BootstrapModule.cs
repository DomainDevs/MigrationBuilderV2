using System.Reflection;

namespace DataToolkit.Bootstrap.Discovery;

internal readonly record struct BootstrapModule
{
    internal BootstrapModule(
        Assembly assembly,
        string rootNamespace,
        string targetNamespace)
    {
        Assembly = assembly;
        RootNamespace = rootNamespace;
        TargetNamespace = targetNamespace;
        RootNamespacePrefix = rootNamespace + ".";
    }

    internal Assembly Assembly { get; }

    internal string RootNamespace { get; }

    internal string RootNamespacePrefix { get; }

    internal string TargetNamespace { get; }
}
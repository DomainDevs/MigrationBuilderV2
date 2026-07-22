using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DataToolkit.Bootstrap.Discovery;

internal static class RegistrationReader
{
    private const string MethodName = "ConfigureServices";

    internal static ServiceRegistration Read(Type implementation)
    {
        ServiceRegistration registration = new();

        MethodInfo? method = implementation.GetMethod(
            MethodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method is null)
        {
            return registration;
        }

        if (method.ReturnType != typeof(KeyValuePair<string, string>[]))
        {
            return registration;
        }

        KeyValuePair<string, string>[]? metadata;

        try
        {
            metadata = method.Invoke(null, null) as KeyValuePair<string, string>[];
        }
        catch
        {
            return registration;
        }

        if (metadata is null)
        {
            return registration;
        }

        foreach (KeyValuePair<string, string> item in metadata)
        {
            string key = item.Key;
            string value = item.Value;

            if (string.IsNullOrEmpty(key) ||
                string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (key.Equals("Lifetime", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Equals("Scoped", StringComparison.OrdinalIgnoreCase))
                {
                    registration.Lifetime = ServiceLifetime.Scoped;
                }
                else if (value.Equals("Singleton", StringComparison.OrdinalIgnoreCase))
                {
                    registration.Lifetime = ServiceLifetime.Singleton;
                }
                else if (value.Equals("Transient", StringComparison.OrdinalIgnoreCase))
                {
                    registration.Lifetime = ServiceLifetime.Transient;
                }

                continue;
            }

            if (key.Equals("Priority", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, out int priority))
                {
                    registration.Priority = priority;
                }

                continue;
            }

            if (key.Equals("Exclude", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(value, out bool exclude))
                {
                    registration.Exclude = exclude;
                }

                continue;
            }

            if (key.Equals("AsSelf", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(value, out bool asSelf))
                {
                    registration.RegisterAsSelf = asSelf;
                }
            }
        }

        return registration;
    }
}
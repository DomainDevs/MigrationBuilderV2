using DataToolkit.Bootstrap.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace DataToolkit.Bootstrap.Discovery;

internal static class BootstrapRegistrar
{
    internal static void Register(
        IServiceCollection services,
        bool verbose,
        IEnumerable<CandidateType> candidates)
    {
        BootstrapConsole.Header();

        Stopwatch stopwatch = Stopwatch.StartNew();

        List<CandidateType> registrations =
            candidates as List<CandidateType> ?? new(candidates);

        registrations.Sort(static (x, y) =>
            x.Registration.Priority.CompareTo(y.Registration.Priority));

        int registered = 0;

        foreach (CandidateType candidate in registrations)
        {
            Type implementation = candidate.Implementation;

            ServiceLifetime lifetime =
                candidate.Registration.Lifetime;

            string lifetimeName =
                lifetime.ToString();

            bool wasRegistered = false;
            bool registeredAsSelf = false;

            IReadOnlyList<Type> servicesToRegister =
                candidate.Services;

            // Registrar por interfaces (si existen)
            foreach (Type service in servicesToRegister)
            {
                services.Add(new ServiceDescriptor(
                    service,
                    implementation,
                    lifetime));

                if (verbose)
                {
                    BootstrapConsole.Registered(
                        service,
                        implementation,
                        lifetimeName);
                }

                wasRegistered = true;
            }

            // Si no tiene interfaces, registrar la implementación.
            if (servicesToRegister.Count == 0)
            {
                services.Add(new ServiceDescriptor(
                    implementation,
                    implementation,
                    lifetime));

                if (verbose)
                {
                    BootstrapConsole.Registered(
                        implementation,
                        implementation,
                        lifetimeName);
                }

                wasRegistered = true;
                registeredAsSelf = true;
            }

            // Si el usuario pidió RegisterAsSelf,
            // registrar también la implementación,
            // evitando duplicados cuando ya fue registrada automáticamente.
            if (candidate.Registration.RegisterAsSelf &&
                !registeredAsSelf)
            {
                services.Add(new ServiceDescriptor(
                    implementation,
                    implementation,
                    lifetime));

                if (verbose)
                {
                    BootstrapConsole.Registered(
                        implementation,
                        implementation,
                        lifetimeName);
                }

                wasRegistered = true;
            }

            if (wasRegistered)
            {
                registered++;
            }
        }

        stopwatch.Stop();

        BootstrapConsole.Summary(
            registered,
            0,
            stopwatch.Elapsed.TotalNanoseconds);
    }
}
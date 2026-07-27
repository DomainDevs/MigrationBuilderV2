using DataToolkit.Bootstrap.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Bootstrap.Discovery;

internal static class BootstrapRegistrar
{
    internal static void Register(
        IServiceCollection services,
        bool verbose,
        IEnumerable<CandidateType> candidates,
        BootstrapProfiler profiler,
        int excluded)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(profiler);

        BootstrapConsole.Header();

        profiler.Start(BootstrapPhase.DescriptorBuild);
        // Cambia por BootstrapPhase.Preparation cuando renombres el enum.

        List<CandidateType> registrations =
            candidates as List<CandidateType> ?? new(candidates);

        registrations.Sort(static (x, y) =>
            x.Registration.Priority.CompareTo(y.Registration.Priority));

        profiler.Stop();

        profiler.Start(BootstrapPhase.DiRegistration);

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

        profiler.Stop();

        BootstrapConsole.Summary(
            registered,
            excluded,
            profiler.Total.TotalNanoseconds);

        BootstrapConsole.Performance(
            profiler);
    }
}
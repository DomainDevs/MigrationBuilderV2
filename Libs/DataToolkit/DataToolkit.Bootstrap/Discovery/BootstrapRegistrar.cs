using DataToolkit.Bootstrap.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace DataToolkit.Bootstrap.Discovery;

internal static class BootstrapRegistrar
{
    internal static void Register(
        IServiceCollection services,
        bool Verbose,
        IEnumerable<CandidateType> candidates)
    {
        BootstrapConsole.Header();

        Stopwatch stopwatch = Stopwatch.StartNew();

        List<CandidateType> registrations =
            candidates as List<CandidateType> ?? new(candidates);

        registrations.Sort(static (x, y) =>
            x.Registration.Priority.CompareTo(y.Registration.Priority));

        int registered = 0;

        for (int i = 0; i < registrations.Count; i++)
        {
            CandidateType candidate = registrations[i];

            Type implementation = candidate.Implementation;

            ServiceLifetime lifetime =
                candidate.Registration.Lifetime;

            string lifetimeName =
                lifetime.ToString();

            bool wasRegistered = false;

            IReadOnlyList<Type> servicesToRegister =
                candidate.Services;

            for (int j = 0; j < servicesToRegister.Count; j++)
            {
                Type service = servicesToRegister[j];

                services.Add(new ServiceDescriptor(
                    service,
                    implementation,
                    lifetime));

                if(Verbose)
                BootstrapConsole.Registered(
                    service,
                    implementation,
                    lifetimeName);

                wasRegistered = true;
            }

            if (candidate.Registration.RegisterAsSelf)
            {
                services.Add(new ServiceDescriptor(
                    implementation,
                    implementation,
                    lifetime));

                BootstrapConsole.Registered(
                    implementation,
                    implementation,
                    lifetimeName);

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
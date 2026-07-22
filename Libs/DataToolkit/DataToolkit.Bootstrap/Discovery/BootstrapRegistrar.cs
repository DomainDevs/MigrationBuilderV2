using DataToolkit.Bootstrap.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace DataToolkit.Bootstrap.Discovery;

internal static class BootstrapRegistrar
{
    internal static void Register(
        IServiceCollection services,
        IEnumerable<CandidateType> candidates)
    {
        BootstrapConsole.Header();

        var stopwatch = Stopwatch.StartNew();

        List<CandidateType> registrations =
            candidates as List<CandidateType> ?? new(candidates);

        registrations.Sort(static (x, y) =>
            x.Registration.Priority.CompareTo(y.Registration.Priority));

        int registered = 0;

        foreach (var candidate in registrations)
        {
            bool wasRegistered = false;
            string? lifetimeName = null;

            foreach (Type service in candidate.Services)
            {
                services.Add(new ServiceDescriptor(
                    service,
                    candidate.Implementation,
                    candidate.Registration.Lifetime));

                lifetimeName ??= candidate.Registration.Lifetime.ToString();

                BootstrapConsole.Registered(
                    service.Name,
                    candidate.Implementation.Name,
                    lifetimeName);

                wasRegistered = true;
            }

            if (candidate.Registration.RegisterAsSelf)
            {
                services.Add(new ServiceDescriptor(
                    candidate.Implementation,
                    candidate.Implementation,
                    candidate.Registration.Lifetime));

                lifetimeName ??= candidate.Registration.Lifetime.ToString();

                BootstrapConsole.Registered(
                    candidate.Implementation.Name,
                    candidate.Implementation.Name,
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
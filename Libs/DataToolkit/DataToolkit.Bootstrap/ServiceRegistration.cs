using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Bootstrap.Discovery;

internal sealed class ServiceRegistration
{
    internal ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    internal int Priority { get; set; } = 1000;

    internal bool RegisterAsSelf { get; set; }

    internal bool Exclude { get; set; }
}
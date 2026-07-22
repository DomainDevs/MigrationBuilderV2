using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Extensions.Resilience;
using DataToolkit.Provider.Sybase.Connections.Providers;
using DataToolkit.Provider.Sybase.Resilience;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Provider.Sybase.Extensions;

public static class SybaseServiceCollectionExtensions
{
    public static IServiceCollection AddDataToolkitSybase(
        this IServiceCollection services)
    {
        services.AddSingleton<IRetryPolicy>(sp =>
        {
            var opt =
                sp.GetRequiredService<DataToolkitOptions>();

            return new SybaseRetryPolicy(
                opt.Retry.MaxRetries,
                opt.Retry.BaseDelayMs);
        });

        services.AddScoped<IDbConnectionFactory,
            SybaseConnectionFactory>();

        return services;
    }
}
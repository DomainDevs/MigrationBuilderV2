using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Provider.SqlServer.Resilience;
using DataToolkit.Provider.SqlServer.Connections.Providers;
using Microsoft.Extensions.DependencyInjection;
using DataToolkit.Library.Extensions.Resilience;

namespace DataToolkit.Provider.SqlServer.Extensions;

public static class SqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddDataToolkitSqlServer(
        this IServiceCollection services)
    {
        services.AddSingleton<IRetryPolicy>(sp =>
        {
            var opt =
                sp.GetRequiredService<DataToolkitOptions>();

            return new SqlRetryPolicy(
                opt.Retry.Enabled,
                opt.Retry.MaxRetries,
                opt.Retry.BaseDelayMs
                );
        });

        services.AddScoped<IDbConnectionFactory,
            SqlServerConnectionFactory>();

        return services;
    }
}
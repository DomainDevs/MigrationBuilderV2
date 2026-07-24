using DataToolkit.Library.Common;
using DataToolkit.Library.Connections.Providers;
using DataToolkit.Library.Extensions.Resilience;
using DataToolkit.Provider.SqlServer.Connections.Providers;
using DataToolkit.Provider.SqlServer.Resilience;
using Microsoft.Extensions.DependencyInjection;

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

        //services.AddScoped<IDbConnectionFactory, SqlServerConnectionFactory>();
        services.AddScoped<IDatabaseProvider, SqlServerProvider>();

        return services;
    }
}
using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Extensions.Resilience;
using DataToolkit.Provider.Sqlite.Connections.Providers;
using DataToolkit.Provider.Sqlite.Resilience;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Provider.Sqlite.Extensions;

public static class SqliteServiceCollectionExtensions
{
    public static IServiceCollection AddDataToolkitSqlite(
        this IServiceCollection services)
    {
        services.AddSingleton<IRetryPolicy>(sp =>
        {
            var opt =
                sp.GetRequiredService<DataToolkitOptions>();

            return new SqliteRetryPolicy(
                opt.Retry.MaxRetries,
                opt.Retry.BaseDelayMs);
        });

        services.AddScoped<
            IDbConnectionFactory,
            SqliteConnectionFactory>();

        return services;
    }
}
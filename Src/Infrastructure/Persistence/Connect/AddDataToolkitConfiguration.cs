using DataToolkit.Library.Connections;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.Extensions;
using DataToolkit.Provider.Sqlite.Extensions;
using DataToolkit.Provider.SqlServer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Connect.Context;

namespace Persistence.Connect;

public static class AddDataToolkitConfiguration
{
    public static IServiceCollection AddBuilderDataToolkit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<DatabaseConnectionFactory>();

        services.AddScoped<IDbConnectionFactory>(sp =>
            sp.GetRequiredService<DatabaseConnectionFactory>());

        services.AddScoped<IDatabaseContext>(sp =>
        {
            var factory = sp.GetRequiredService<IDbConnectionFactory>();

            return new DatabaseContext(
                factory,
                configuration);
        });

        //Registra la libreria
        ////services.AddDataToolkit(configuration);

        // Registra el proveedor SQL Server.
        services.AddDataToolkitSqlServer();

        // Registra el proveedor SQl Lite
        services.AddDataToolkitSqlite();

        ////services.AddScoped<SqlServerContext>();
        ////services.AddScoped<SqliteContext>();


        return services;
    }
}
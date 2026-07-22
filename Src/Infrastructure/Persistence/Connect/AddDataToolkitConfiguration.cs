using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Persistence.Connect.Context;
using DataToolkit.Library.Extensions;
using DataToolkit.Provider.SqlServer.Extensions;
using DataToolkit.Provider.Sqlite.Extensions;

namespace Persistence.Connect;

public static class AddDataToolkitConfiguration
{
    public static IServiceCollection AddBuilderDataToolkit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        //Registra la libreria
        services.AddDataToolkit(configuration);

        // Registra el proveedor SQL Server.
        services.AddDataToolkitSqlServer();

        // Registra el proveedor SQl Lite
        services.AddDataToolkitSqlite();

        services.AddScoped<SqlServerContext>();
        services.AddScoped<SqliteContext>();

        return services;
    }
}
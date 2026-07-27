using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Connect;
using Persistence.Database;
using DataToolkit.BulkTransfer.DependencyInjection;

namespace Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registra DataToolkit (librería de conexión) y tus repositorios.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services,
        IConfiguration config,
        bool enableVerboseLogs = false)
    {
        // 1. Base de datos de historico
        services.AddSingleton<SqliteDatabaseInitializer>();

        // 2. Configuración de motor/conexión
        services.AddBuilderDataToolkit(config);

        //3. Add bulk
        services.AddBulkTransfer();
        
        //4. Repositorios (Automatico boostrap).

        return services;
    }

    public static async Task UsePersistenceAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var initializer = scope.ServiceProvider
            .GetRequiredService<SqliteDatabaseInitializer>();

        await initializer.InitializeAsync();
    }
}

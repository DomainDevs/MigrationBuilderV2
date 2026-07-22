using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Connect;
using Persistence.Database;

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

        // 3. Registro de Repositorios 
        //Visualizar diagnóstico si isDev = true
        //y enableVerboseLogs = true para decidir si realmente se imprime
        //services.AddRepositories(enableVerboseLogs); //lo hace boostrap

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

using Infrastructure.Cors;
using Infrastructure.Documentation.Extensions;
using Infrastructure.ErrorHandling;
using Infrastructure.ProjectSystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registra los componentes necesario de la infraestructura
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {

        services
            .AddOpenApiDocumentation(config)            // Configura Swagger / OpenAPI
            .AddCorsPolicy(config)                      // Configura políticas de
            .AddMemoryCache()
            .AddSystem()
            .AddHttpClient();


        return services;
    }

    public static IApplicationBuilder UseInfrastructure(
        this IApplicationBuilder builder, IConfiguration config)
    {

        IWebHostEnvironment environment =
            builder.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        if (config.GetValue<bool>("SwaggerSettings:Enabled"))
        {
            builder.UseOpenApiDocumentation();
        }

        // Ejecutamos la cadena base
        builder
            .UseHttpsRedirection()
            .UseErrorHandler()
            .UseRouting()
            .UseConfiguredCors()
            .UseAuthentication()
            .UseAuthorization();

        return builder; // <--- Ahora el return es EXPLÍCITO y necesario
    }

}
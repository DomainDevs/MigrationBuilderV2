using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Infrastructure.Documentation;

public static class AddConfiguredSwagger
{
    internal static IServiceCollection AddOpenApiDocumentation(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Migration API",
                Version = "v1"
            });
        });

        return services;
    }

    public static IApplicationBuilder UseOpenApiDocumentation(
        this IApplicationBuilder app,
        IConfiguration config)
    {
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            //c.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration API v1");
            //c.SwaggerEndpoint("v1/swagger.json", "Migration API v1");
            c.SwaggerEndpoint("./v1/swagger.json", "Migration API v1");

        });

        return app;
    }
}
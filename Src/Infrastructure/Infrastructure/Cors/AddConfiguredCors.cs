using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;


namespace Infrastructure.Cors;

//la clase de Puesta en marcha, tiene como objetivo
internal static class AddConfiguredCors
{
    private const string CorsPolicy = nameof(CorsPolicy);

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var corsSettings = config.GetSection(nameof(CorsSettings)).Get<CorsSettings>() ?? new CorsSettings();
        var origins = new List<string>();

        if (!string.IsNullOrWhiteSpace(corsSettings.Web))
            origins.AddRange(corsSettings.Web.Split(';', StringSplitOptions.RemoveEmptyEntries));

        if (!string.IsNullOrWhiteSpace(corsSettings.Mobile))
            origins.AddRange(corsSettings.Mobile.Split(';', StringSplitOptions.RemoveEmptyEntries));

        if (!string.IsNullOrWhiteSpace(corsSettings.Api))
            origins.AddRange(corsSettings.Api.Split(';', StringSplitOptions.RemoveEmptyEntries));

        // Esta función limpia espacios en blanco que puedan venir del JSON
        void ParseAndAdd(string? setting)
        {
            if (!string.IsNullOrWhiteSpace(setting))
            {
                var parts = setting.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(o => o.Trim()); // <--- ESTO ES LO CLAVE
                origins.AddRange(parts);
            }
        }

        ParseAndAdd(corsSettings.Web);
        ParseAndAdd(corsSettings.Mobile);
        ParseAndAdd(corsSettings.Api);

        services.AddCors(opt =>
            opt.AddPolicy(CorsPolicy, policy =>
                policy.WithOrigins(origins.Where(o => !string.IsNullOrWhiteSpace(o)) // Quita nulos
                                          .Select(o => o.Trim().TrimEnd('/'))        // Limpia espacios y slashes
                                          .Distinct()                               // ¡Quita los duplicados!
                                          .ToArray())
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));

        return services;
    }

    public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicy);
    }
}
using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Extensions.Resilience;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DataToolkit.Library.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configuración basada en appsettings.json.
    /// </summary>
    public static IServiceCollection AddDataToolkit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DataToolkitOptions>(
            configuration.GetSection(
                DataToolkitOptions.SectionName));

        return RegisterServices(services);
    }

    /// <summary>
    /// Configuración programática.
    /// </summary>
    public static IServiceCollection AddDataToolkit(
        this IServiceCollection services,
        Action<DataToolkitOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return RegisterServices(services);
    }

    /// <summary>
    /// Registro común de servicios.
    /// </summary>
    private static IServiceCollection RegisterServices(
        IServiceCollection services)
    {
        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<DataToolkitOptions>>().Value);

        services.AddSingleton<RetryExecutor>();

        services.AddScoped<IUnitOfWork>(sp =>
        {
            var factory =
                sp.GetRequiredService<IDbConnectionFactory>();

            var options =
                sp.GetRequiredService<DataToolkitOptions>();

            RetryExecutor? retryExecutor = null;

            if (options.Retry.Enabled)
            {
                retryExecutor =
                    sp.GetRequiredService<RetryExecutor>();
            }

            return new UnitOfWork(
                factory,
                options.DefaultConnectionAlias
                );
        });

        return services;
    }
}
using Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        // 1. Registro automático de MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // 2. Registro de Validaciones (FluentValidation)
        services.AddValidatorsFromAssembly(assembly);

        // 3. Registro del Pipeline de Validación (El "filtro" de seguridad)
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>) // Ya no necesitas la ruta larga
        );

        /*Lo hace boostrap
        // 4. Registro de servicios (Scrutor)
        // Diagnóstico (isDev & enableVerboseLogs y filtro por nombre filter)
        services.AddServices(
            isDev,
            enableVerboseLogs,
            filter);
        */

        return services;
    }
}
using Application.Common.Behaviors;
using FluentValidation;
using MediatR;
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

        //4. Servicios de aplicacion (Automatico boostrap).

        return services;
    }
}
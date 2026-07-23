using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.System;

internal static class AddSystemExtensions
{
    internal static IServiceCollection AddSystem(
        this IServiceCollection services)
    {
        services.AddTransient<IMigrationProjectInitializer, MigrationProjectInitializer>();

        return services;
    }
}
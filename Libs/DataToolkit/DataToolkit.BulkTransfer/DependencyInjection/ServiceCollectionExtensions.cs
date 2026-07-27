using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Connections;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.BulkTransfer.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.BulkTransfer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBulkTransfer(this IServiceCollection services)
    {
        // Connection Factory
        services.AddSingleton<
            IBulkConnectionFactory,
            BulkConnectionFactory>();

        // Internal components
        services.AddSingleton<
            IBulkTransferValidator,
            BulkSchemaValidator>();

        services.AddTransient<
            IBulkTransferReader, 
            DbTransferReader>();

        services.AddTransient<
            IBulkTransferWriter, 
            SqlServerBulkWriter>();

        // Internal engine
        services.AddTransient<
            IBulkTransferEngine,
            BulkTransferEngine>();

        // Public API
        services.AddTransient<
            IBulkTransferService, 
            BulkTransferService>();


        return services;
    }
}

using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace DataToolkit.BulkTransfer.Connections;

internal sealed class BulkConnectionFactory : IBulkConnectionFactory
{
    public DbConnection Create(BulkDataSource source)
    {
        return source.Provider switch
        {
            BulkProvider.SqlServer => new SqlConnection(source.ConnectionString),
            _ => throw new NotSupportedException(
                $"Provider '{source.Provider}' is not supported.")
        };
    }
}

using System.Data.Common;
using Microsoft.Data.SqlClient;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer.SqlServer;

public sealed class SqlServerBulkWriter:IBulkTransferWriter
{
    public async Task WriteAsync(
        DbDataReader reader,
        DbConnection connection,
        TableMetadata target,
        CancellationToken ct=default)
    {
        using var bulk=new SqlBulkCopy((SqlConnection)connection);
        bulk.DestinationTableName=$"[{target.Schema}].[{target.Name}]";
        BulkMappingBuilder.Build(bulk, target);
        await bulk.WriteToServerAsync(reader,ct);
    }
}

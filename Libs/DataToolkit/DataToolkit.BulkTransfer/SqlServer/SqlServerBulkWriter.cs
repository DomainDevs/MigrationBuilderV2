using System.Data.Common;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;
using Microsoft.Data.SqlClient;

namespace DataToolkit.BulkTransfer.SqlServer;

public sealed class SqlServerBulkWriter : IBulkTransferWriter
{
    public async Task WriteAsync(
        DbDataReader reader,
        DbConnection connection,
        TableMetadata target,
        BulkTransferOptions options,
        CancellationToken cancellationToken = default)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new BulkTransferException("The supplied connection must be a SqlConnection.");
        }

        using SqlBulkCopy bulk = new SqlBulkCopy(sqlConnection);

        bulk.DestinationTableName = $"[{target.Schema}].[{target.Name}]";
        bulk.BatchSize = options.BatchSize;
        bulk.BulkCopyTimeout = options.Timeout;

        BulkMappingBuilder.Build(bulk, target);

        await bulk.WriteToServerAsync(reader, cancellationToken);
    }
}
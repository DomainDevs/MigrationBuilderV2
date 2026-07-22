using DataToolkit.Library;
using System.Data.Common;

namespace DataToolkit.BulkTransfer.Abstractions;

public interface IBulkTransferEngine
{
    Task<Core.BulkTransferResult> TransferAsync(
        DbConnection sourceConnection,
        DbConnection targetConnection,
        string source,
        TableMetadata targetTable,
        CancellationToken cancellationToken = default);

    Task<Core.BulkTransferResult> TransferAsync(
        DbConnection sourceConnection,
        DbConnection targetConnection,
        TableMetadata sourceTable,
        TableMetadata targetTable,
        CancellationToken cancellationToken = default);
}

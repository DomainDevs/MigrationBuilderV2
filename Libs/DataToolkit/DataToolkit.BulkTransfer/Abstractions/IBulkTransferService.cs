using DataToolkit.BulkTransfer.Connections;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer.Abstractions;

public interface IBulkTransferService
{
    Task<BulkTransferResult> TransferAsync(
        BulkDataSource source,
        BulkDataSource target,
        string extractionQuery,
        TableMetadata targetTable,
        CancellationToken cancellationToken = default);

    Task<BulkTransferResult> TransferAsync(
        BulkDataSource source,
        BulkDataSource target,
        TableMetadata sourceTable,
        TableMetadata targetTable,
        CancellationToken cancellationToken = default);
}

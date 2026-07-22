using System.Data.Common;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Connections;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer.Services;

public sealed class BulkTransferService : IBulkTransferService
{
    private readonly IBulkTransferEngine _engine;
    private readonly IBulkConnectionFactory _factory;

    public BulkTransferService(
        IBulkTransferEngine engine,
        IBulkConnectionFactory factory)
    {
        _engine = engine;
        _factory = factory;
    }

    public async Task<BulkTransferResult> TransferAsync(
        BulkDataSource source,
        BulkDataSource target,
        string extractionQuery,
        TableMetadata targetTable,
        CancellationToken cancellationToken = default)
    {
        await using DbConnection sourceConnection = _factory.Create(source);
        await using DbConnection targetConnection = _factory.Create(target);

        await sourceConnection.OpenAsync(cancellationToken);
        await targetConnection.OpenAsync(cancellationToken);

        return await _engine.TransferAsync(
            sourceConnection,
            targetConnection,
            extractionQuery,
            targetTable,
            cancellationToken);
    }

    public async Task<BulkTransferResult> TransferAsync(
        BulkDataSource source,
        BulkDataSource target,
        TableMetadata sourceTable,
        TableMetadata targetTable,
        CancellationToken cancellationToken = default)
    {
        await using DbConnection sourceConnection = _factory.Create(source);
        await using DbConnection targetConnection = _factory.Create(target);

        await sourceConnection.OpenAsync(cancellationToken);
        await targetConnection.OpenAsync(cancellationToken);

        return await _engine.TransferAsync(
            sourceConnection,
            targetConnection,
            sourceTable,
            targetTable,
            cancellationToken);
    }
}

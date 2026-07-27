using System.Data.Common;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer;

public sealed class BulkTransferService : IBulkTransferService
{
    private readonly IBulkTransferEngine _engine;
    private BulkTransferOptions _options;

    public BulkTransferService(
        IBulkTransferEngine engine)
    {
        _engine = engine;
        _options = new BulkTransferOptions();
    }

    public IBulkTransferService WithOptions(
        Action<BulkTransferOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BulkTransferOptions
        {
            Timeout = _options.Timeout

            //BatchSize = _options.BatchSize,
            //BulkCopyTimeout = _options.BulkCopyTimeout,
            //ExtractionTimeout = _options.ExtractionTimeout
        };

        configure(options);

        _options = options;

        return this;
    }

    public async Task TransferAsync(
        DbConnection source,
        DbConnection destination,
        string sql,
        TableMetadata target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(target);

        await _engine.TransferAsync(
            source,
            destination,
            sql,
            target,
            _options,
            cancellationToken);
    }

    public async Task TransferTableAsync(
        DbConnection source,
        DbConnection destination,
        string sourceTable,
        TableMetadata target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTable);
        ArgumentNullException.ThrowIfNull(target);

        await _engine.TransferAsync(
            source,
            destination,
            sourceTable,
            target,
            _options,
            cancellationToken);
    }
}
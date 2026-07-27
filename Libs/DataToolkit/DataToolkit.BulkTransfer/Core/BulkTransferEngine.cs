using System.Data.Common;
using System.Diagnostics;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer.Core;

public sealed class BulkTransferEngine : IBulkTransferEngine
{
    private readonly IBulkTransferValidator _validator;
    private readonly IBulkTransferReader _reader;
    private readonly IBulkTransferWriter _writer;

    public BulkTransferEngine(
        IBulkTransferValidator validator,
        IBulkTransferReader reader,
        IBulkTransferWriter writer)
    {
        _validator = validator;
        _reader = reader;
        _writer = writer;
    }

    public Task<BulkTransferResult> TransferAsync(
        DbConnection source,
        DbConnection target,
        TableMetadata sourceTable,
        TableMetadata targetTable,
        BulkTransferOptions options,
        CancellationToken cancellationToken = default)
    {
        return TransferAsync(
            source,
            target,
            $"[{sourceTable.Schema}].[{sourceTable.Name}]",
            targetTable,
            options,
            cancellationToken);
    }

    public async Task<BulkTransferResult> TransferAsync(
        DbConnection source,
        DbConnection target,
        string extraction,
        TableMetadata targetTable,
        BulkTransferOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetTable);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(extraction);

        var stopwatch = Stopwatch.StartNew();

        var sql = BulkSqlBuilder.Normalize(extraction);

        await using var reader = await _reader.ExecuteReaderAsync(
            source,
            sql,
            //options,
            cancellationToken);

        _validator.Validate(reader, targetTable);

        await _writer.WriteAsync(
            reader,
            target,
            targetTable,
            options,
            cancellationToken);

        stopwatch.Stop();

        return new BulkTransferResult
        {
            Success = true,
            Duration = stopwatch.Elapsed
        };
    }
}
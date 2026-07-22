using System.Data.Common;
using System.Diagnostics;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer.Core;

public sealed class BulkTransferEngine:IBulkTransferEngine
{
    private readonly IBulkTransferValidator _validator;
    private readonly IBulkTransferReader _reader;
    private readonly IBulkTransferWriter _writer;

    public BulkTransferEngine(
        IBulkTransferValidator validator,
        IBulkTransferReader reader,
        IBulkTransferWriter writer)
    {
        _validator=validator;
        _reader=reader;
        _writer=writer;
    }

    public Task<BulkTransferResult> TransferAsync(
        DbConnection source,
        DbConnection target,
        TableMetadata sourceTable,
        TableMetadata targetTable,
        CancellationToken ct=default)
        =>TransferAsync(source,target,$"[{sourceTable.Schema}].[{sourceTable.Name}]",targetTable,ct);

    public async Task<BulkTransferResult> TransferAsync(
        DbConnection source,
        DbConnection target,
        string extraction,
        TableMetadata targetTable,
        CancellationToken ct=default)
    {
        var sw=Stopwatch.StartNew();
        var sql=BulkSqlBuilder.Normalize(extraction);

        await using var reader=await _reader.ExecuteReaderAsync(source,sql,ct);
        _validator.Validate(reader,targetTable);
        await _writer.WriteAsync(reader,target,targetTable,ct);

        sw.Stop();
        return new BulkTransferResult{Success=true,Duration=sw.Elapsed};
    }
}

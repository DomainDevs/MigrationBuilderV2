using System.Data.Common;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;

namespace DataToolkit.BulkTransfer.Abstractions;

public interface IBulkTransferEngine
{
    Task<BulkTransferResult> TransferAsync(
		DbConnection source, 
		DbConnection target, 
		string extraction, 
		TableMetadata targetTable, 
		BulkTransferOptions options, 
		CancellationToken cancellationToken = default);
    Task<BulkTransferResult> TransferAsync(
		DbConnection source, 
		DbConnection target, 
		TableMetadata sourceTable, 
		TableMetadata targetTable, 
		BulkTransferOptions options, 
		CancellationToken cancellationToken = default);
}

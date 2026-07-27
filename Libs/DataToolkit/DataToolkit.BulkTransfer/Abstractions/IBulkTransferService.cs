using DataToolkit.BulkTransfer.Connections;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;
using System.Data.Common;

namespace DataToolkit.BulkTransfer.Abstractions;

public interface IBulkTransferService
{
    Task TransferAsync(
		DbConnection source, 
		DbConnection destination, 
		string sql, 
		TableMetadata target, 
		CancellationToken cancellationToken = default);
    Task TransferTableAsync(
		DbConnection source, 
		DbConnection destination, 
		string sourceTable, 
		TableMetadata target, 
		CancellationToken cancellationToken = default);
    IBulkTransferService WithOptions(Action<BulkTransferOptions> configure);
}

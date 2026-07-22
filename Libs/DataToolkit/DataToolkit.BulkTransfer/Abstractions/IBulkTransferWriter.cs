using DataToolkit.Library;
using System.Data.Common;
namespace DataToolkit.BulkTransfer.Abstractions;
public interface IBulkTransferWriter
{
    Task WriteAsync(
        DbDataReader reader,
        DbConnection connection,
        TableMetadata targetTable,
        CancellationToken cancellationToken=default);
}

using System.Data.Common;
namespace DataToolkit.BulkTransfer.Abstractions;
public interface IBulkTransferReader
{
    Task<DbDataReader> ExecuteReaderAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken=default);
}

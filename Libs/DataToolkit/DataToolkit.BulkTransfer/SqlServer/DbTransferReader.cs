using System.Data.Common;
using DataToolkit.BulkTransfer.Abstractions;

namespace DataToolkit.BulkTransfer.SqlServer;
public sealed class DbTransferReader:IBulkTransferReader
{
    public async Task<DbDataReader> ExecuteReaderAsync(DbConnection c,string sql,CancellationToken ct=default)
    {
        var cmd=c.CreateCommand();
        cmd.CommandText=sql;
        return await cmd.ExecuteReaderAsync(ct);
    }
}

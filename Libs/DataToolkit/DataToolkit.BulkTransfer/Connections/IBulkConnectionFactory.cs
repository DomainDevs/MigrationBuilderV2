using System.Data.Common;

namespace DataToolkit.BulkTransfer.Connections;

public interface IBulkConnectionFactory
{
    DbConnection Create(BulkDataSource source);
}

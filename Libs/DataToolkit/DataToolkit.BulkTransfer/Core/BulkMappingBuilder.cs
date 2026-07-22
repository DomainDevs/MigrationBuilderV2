using DataToolkit.Library;
using Microsoft.Data.SqlClient;
namespace DataToolkit.BulkTransfer.Core;
public static class BulkMappingBuilder
{
    public static void Build(SqlBulkCopy bulk,TableMetadata target)
    {
        foreach(var c in target.Columns.Where(x=>!x.IsComputed))
            bulk.ColumnMappings.Add(c.Name,c.Name);
    }
}

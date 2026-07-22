using System.Data.Common;
using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.Library;
namespace DataToolkit.BulkTransfer.Core;
public sealed class BulkSchemaValidator:IBulkTransferValidator
{
    public void Validate(DbDataReader reader,TableMetadata target)
    {
        var cols=Enumerable.Range(0,reader.FieldCount)
            .Select(reader.GetName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach(var c in target.Columns.Where(x=>!x.IsComputed))
            if(!cols.Contains(c.Name))
                throw new BulkTransferException($"Column '{c.Name}' not found in extraction query.");
    }
}

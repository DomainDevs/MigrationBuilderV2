using DataToolkit.Library;
using System.Data.Common;
namespace DataToolkit.BulkTransfer.Abstractions;
public interface IBulkTransferValidator
{
    void Validate(DbDataReader reader, TableMetadata targetTable);
}

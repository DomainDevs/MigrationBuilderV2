using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;
using Microsoft.Extensions.Options;
using System.Data.Common;
namespace DataToolkit.BulkTransfer.Abstractions;
public interface IBulkTransferWriter
{
    Task WriteAsync(DbDataReader reader, 
        DbConnection connection, 
        TableMetadata target, 
        BulkTransferOptions options, 
        CancellationToken cancellationToken = default);
}
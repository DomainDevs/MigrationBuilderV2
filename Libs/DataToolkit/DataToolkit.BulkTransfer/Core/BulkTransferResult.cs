namespace DataToolkit.BulkTransfer.Core;
public sealed class BulkTransferResult
{
    public long RowsCopied {get;set;}
    public TimeSpan Duration {get;set;}
    public bool Success {get;set;}
}

namespace DataToolkit.BulkTransfer.Core;
public class BulkTransferOptions
{
    public int BatchSize {get;set;}=5000;
    public int Timeout {get;set;}= 600;
}

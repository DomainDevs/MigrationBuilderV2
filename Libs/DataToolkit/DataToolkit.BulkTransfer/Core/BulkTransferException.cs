namespace DataToolkit.BulkTransfer.Core;
public sealed class BulkTransferException:Exception
{
    public BulkTransferException(string message,Exception? inner=null):base(message,inner){}
}

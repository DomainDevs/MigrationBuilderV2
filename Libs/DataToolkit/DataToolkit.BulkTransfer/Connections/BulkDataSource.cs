namespace DataToolkit.BulkTransfer.Connections;

public sealed class BulkDataSource
{
    public required BulkProvider Provider { get; init; }

    public required string ConnectionString { get; init; }
}

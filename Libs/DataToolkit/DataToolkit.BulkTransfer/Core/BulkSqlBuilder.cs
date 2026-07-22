namespace DataToolkit.BulkTransfer.Core;
public static class BulkSqlBuilder
{
    public static string Normalize(string source)
    {
        source=source.Trim();
        return source.StartsWith("SELECT",StringComparison.OrdinalIgnoreCase)
            ? source
            : $"SELECT * FROM {source}";
    }
}

namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlJoin(
    string Type,
    string Table,
    string On
) : SqlNode;
namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlGroupBy(List<string> Columns) : SqlNode;
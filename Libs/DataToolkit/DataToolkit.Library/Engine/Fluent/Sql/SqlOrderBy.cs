namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlOrderBy(List<string> Columns) : SqlNode;
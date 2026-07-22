namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlSelect(List<string> Columns) : SqlNode;
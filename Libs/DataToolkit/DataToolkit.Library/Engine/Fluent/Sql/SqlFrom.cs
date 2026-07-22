namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlFrom(List<string> Tables) : SqlNode;
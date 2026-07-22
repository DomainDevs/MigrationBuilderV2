namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlParameter(string Name, object? Value) : SqlNode;
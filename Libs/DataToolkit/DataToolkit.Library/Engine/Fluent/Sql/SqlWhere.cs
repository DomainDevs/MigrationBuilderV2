namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlWhere(SqlNode Expression) : SqlNode;
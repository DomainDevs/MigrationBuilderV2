using System.Xml;

namespace DataToolkit.Library.Fluent.Sql;

internal sealed record SqlBinary(SqlNode Left, string Op, SqlNode Right) : SqlNode;

using DataToolkit.Library.Diagnostics;
using System.Data;

namespace DataToolkit.Library.Engine.Models;

public sealed class SqlStatement
{
    public string CommandText { get; }

    public object? Parameters { get; }

    public CommandType CommandType { get; }

    public int? Timeout { get; }

    public string Preview =>
        SqlPreview.Render(CommandText, Parameters);
}
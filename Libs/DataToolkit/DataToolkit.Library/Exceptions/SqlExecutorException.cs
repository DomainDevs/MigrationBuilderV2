namespace DataToolkit.Library.Exceptions;
public class SqlExecutorException : Exception
{
    public string? SqlQuery { get; }

    public SqlExecutorException(string message, Exception inner, string? sql = null)
        : base(message, inner)
    {
        SqlQuery = sql;
    }
}

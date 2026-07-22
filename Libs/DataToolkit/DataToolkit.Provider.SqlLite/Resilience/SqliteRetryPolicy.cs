using DataToolkit.Library.Extensions.Resilience;
using Microsoft.Data.Sqlite;

namespace DataToolkit.Provider.Sqlite.Resilience;

public sealed class SqliteRetryPolicy : IRetryPolicy
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;

    public SqliteRetryPolicy(
        int maxRetries,
        int baseDelayMs)
    {
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
    }

    public bool ShouldRetry(
        Exception ex,
        int attempt)
    {
        if (attempt >= _maxRetries)
            return false;

        if (ex is not SqliteException sqliteEx)
            return false;

        return sqliteEx.SqliteErrorCode switch
        {
            5 => true, // SQLITE_BUSY (database is locked)
            6 => true, // SQLITE_LOCKED (table is locked)
            _ => false
        };
    }

    public TimeSpan GetDelay(int attempt)
    {
        var delay =
            _baseDelayMs * Math.Pow(2, attempt - 1);

        return TimeSpan.FromMilliseconds(delay);
    }
}
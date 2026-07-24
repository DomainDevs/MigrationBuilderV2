using DataToolkit.Library.Connections.Providers;
using Microsoft.Data.Sqlite;
using System.Data;

namespace DataToolkit.Provider.Sqlite.Connections.Providers;

/// <summary>
/// Proveedor de conexiones para SQLite.
/// </summary>
public sealed class SqliteProvider : IDatabaseProvider
{
    public const string ProviderName = "Sqlite";

    public string Name => ProviderName;

    public IDbConnection CreateConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new SqliteConnection(connectionString);
    }
}
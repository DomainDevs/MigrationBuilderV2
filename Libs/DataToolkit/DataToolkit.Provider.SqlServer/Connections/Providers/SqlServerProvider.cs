using DataToolkit.Library.Connections.Providers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataToolkit.Provider.SqlServer.Connections.Providers;

/// <summary>
/// Proveedor de conexiones para Microsoft SQL Server.
/// </summary>
public sealed class SqlServerProvider : IDatabaseProvider
{
    public const string ProviderName = "SqlServer";

    public string Name => ProviderName;

    public IDbConnection CreateConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new SqlConnection(connectionString);
    }
}
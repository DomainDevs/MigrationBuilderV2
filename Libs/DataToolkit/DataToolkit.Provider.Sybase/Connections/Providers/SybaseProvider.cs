using AdoNetCore.AseClient;
using DataToolkit.Library.Connections.Providers;
using System.Data;

namespace DataToolkit.Provider.Sybase.Connections.Providers;

/// <summary>
/// Proveedor de conexiones para SAP ASE (Sybase).
/// </summary>
public sealed class SybaseProvider : IDatabaseProvider
{
    public const string ProviderName = "Sybase";

    public string Name => ProviderName;

    public IDbConnection CreateConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new AseConnection(connectionString);
    }
}
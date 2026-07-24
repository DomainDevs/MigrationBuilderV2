using System.Data;

namespace DataToolkit.Library.Connections.Providers;

/// <summary>
/// Representa un proveedor capaz de crear conexiones
/// para un motor de base de datos específico.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Nombre único del proveedor.
    /// Ejemplo:
    /// SqlServer
    /// Sqlite
    /// PostgreSql
    /// Oracle
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Crea una conexión utilizando una cadena de conexión.
    /// </summary>
    IDbConnection CreateConnection(string connectionString);
}

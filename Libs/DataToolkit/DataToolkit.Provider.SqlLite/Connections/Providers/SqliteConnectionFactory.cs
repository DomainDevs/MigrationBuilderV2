using DataToolkit.Library.Connections;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataToolkit.Provider.Sqlite.Connections.Providers;

public sealed class SqliteConnectionFactory
    : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqliteConnectionFactory(
        IConfiguration configuration)
    {
        _configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IDbConnection CreateConnection(
        string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);

        // Si el usuario pasó directamente una cadena de conexión,
        // se utiliza sin consultar la configuración.
        
        if (ConnectionHelper.IsConnectionString(connection))
            return new SqliteConnection(connection);

        // De lo contrario, se asume que es un alias definido en ConnectionStrings.
        var connectionString =
            _configuration.GetConnectionString(connection);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No se encontró un alias de conexión llamado '{connection}' en ConnectionStrings.");
        }

        return new SqliteConnection(connectionString);
    }
}
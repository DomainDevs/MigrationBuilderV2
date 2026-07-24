using DataToolkit.Library.Connections.Providers;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataToolkit.Library.Connections;

/// <summary>
/// Fábrica central de conexiones.
/// Resuelve el proveedor configurado y delega la creación
/// de la conexión al proveedor correspondiente.
/// </summary>
public sealed class DatabaseConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyDictionary<string, IDatabaseProvider> _providers;

    public DatabaseConnectionFactory(
        IConfiguration configuration,
        IEnumerable<IDatabaseProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(providers);

        _configuration = configuration;

        var providerList = providers.ToList();

        var duplicate = providerList
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"El proveedor '{duplicate.Key}' está registrado más de una vez.");
        }

        _providers = providerList.ToDictionary(
            p => p.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IDbConnection CreateConnection(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        var connectionString = ResolveConnectionString(alias);
        var provider = ResolveProvider(alias);

        return provider.CreateConnection(connectionString);
    }

    private string ResolveConnectionString(string alias)
    {
        var connectionString =
            _configuration.GetConnectionString(alias);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No existe una cadena de conexión registrada para el alias '{alias}'.");
        }

        return connectionString;
    }

    private IDatabaseProvider ResolveProvider(string alias)
    {
        var providerName =
            _configuration.GetValue<string>($"DatabaseProviders:{alias}");

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException(
                $"No existe un proveedor configurado para el alias '{alias}'.");
        }

        if (!_providers.TryGetValue(providerName, out var provider))
        {
            throw new InvalidOperationException(
                $"No hay un proveedor registrado con el nombre '{providerName}'.");
        }

        return provider;
    }
}
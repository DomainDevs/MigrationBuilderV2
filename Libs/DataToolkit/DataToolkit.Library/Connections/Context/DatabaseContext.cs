using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.Configuration;

namespace DataToolkit.Library.Connections.Context;

/// <summary>
/// Provides dynamic access to all configured databases.
/// </summary>
public sealed class DatabaseContext : IDatabaseContext
{
    private readonly IDbConnectionFactory _factory;
    private readonly HashSet<string> _aliases;

    public DatabaseContext(
        IDbConnectionFactory factory,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(configuration);

        _factory = factory;

        _aliases = configuration
            .GetSection("Connections")
            .GetChildren()
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public IUnitOfWork this[string alias]
        => Create(alias);

    /// <inheritdoc/>
    public bool Contains(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        return _aliases.Contains(alias);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Aliases
        => _aliases;

    private IUnitOfWork Create(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        if (!_aliases.Contains(alias))
        {
            throw new KeyNotFoundException(
                $"Database alias '{alias}' is not configured.");
        }

        return new UnitOfWork(
            _factory,
            alias);
    }
}
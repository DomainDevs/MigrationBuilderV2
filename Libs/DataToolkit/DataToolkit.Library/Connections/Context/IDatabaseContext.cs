using DataToolkit.Library.UnitOfWorkLayer;

namespace DataToolkit.Library.Connections.Context;

/// <summary>
/// Provides access to the databases configured in the application.
/// </summary>
public interface IDatabaseContext
{
    /// <summary>
    /// Creates a new <see cref="IUnitOfWork"/> for the specified database alias.
    /// </summary>
    /// <param name="alias">Database alias.</param>
    IUnitOfWork this[string alias] { get; }

    /// <summary>
    /// Determines whether the specified database alias exists.
    /// </summary>
    /// <param name="alias">Database alias.</param>
    bool Contains(string alias);

    /// <summary>
    /// Gets all configured database aliases.
    /// </summary>
    IReadOnlyCollection<string> Aliases { get; }
}
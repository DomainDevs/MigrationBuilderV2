namespace DataToolkit.Library.ChangeTracking;

internal sealed class ChangeTracker : IChangeTracker
{
    private readonly Dictionary<object, object> _entries = new();

    public EntityEntry<T> Track<T>(T entity) where T : class
    {
        var entry = new EntityEntry<T>(entity);
        _entries[entity] = entry;
        return entry;
    }

    public EntityEntry<T>? Entry<T>(T entity) where T : class
        => _entries.TryGetValue(entity, out var entry)
            ? (EntityEntry<T>)entry
            : null;

    public IReadOnlyCollection<object> TrackedEntities => _entries.Keys.ToList();

    public void Clear()
    {
        _entries.Clear();
    }
}
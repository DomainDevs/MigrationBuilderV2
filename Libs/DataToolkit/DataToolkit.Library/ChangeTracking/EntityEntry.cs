using System.Linq.Expressions;

namespace DataToolkit.Library.ChangeTracking;

internal sealed class EntityEntry<T> where T : class
{
    private readonly Dictionary<string, object?> _originalValues = new();
    private readonly HashSet<string> _modifiedProperties = new();

    public EntityEntry(T original)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));

        foreach (var prop in typeof(T).GetProperties())
            _originalValues[prop.Name] = prop.GetValue(original);
    }

    public IReadOnlyCollection<string> ModifiedProperties
        => _modifiedProperties;

    public bool IsModified
        => _modifiedProperties.Count > 0;

    public IReadOnlyCollection<string> DetectChanges(T current)
    {
        if (current == null)
            throw new ArgumentNullException(nameof(current));

        // IMPORTANTE:
        // recalcula desde cero cada vez
        _modifiedProperties.Clear();

        foreach (var prop in typeof(T).GetProperties())
        {
            var original =
                _originalValues.TryGetValue(prop.Name, out var o)
                    ? o
                    : null;

            var currentValue =
                prop.GetValue(current);

            if (!Equals(original, currentValue))
                _modifiedProperties.Add(prop.Name);
        }

        return _modifiedProperties;
    }

    public void AcceptChanges()
    {
        _modifiedProperties.Clear();
    }

    public void Property(Expression<Func<T, object>> property)
    {
        var name = GetPropertyName(property);

        _modifiedProperties.Add(name);
    }

    private static string GetPropertyName(
        Expression<Func<T, object>> expr)
    {
        if (expr.Body is MemberExpression m)
            return m.Member.Name;

        if (expr.Body is UnaryExpression u &&
            u.Operand is MemberExpression m2)
            return m2.Member.Name;

        throw new ArgumentException(
            "La expresión debe ser una propiedad.");
    }
}
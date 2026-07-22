using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace DataToolkit.Library.Common.Metadata;

internal static class EntityMetadataHelper
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata>
        _metadataCache = new();

    public static EntityMetadata GetMetadata<T>()
        where T : class
        => GetMetadata(typeof(T));

    public static EntityMetadata GetMetadata(Type type)
    {
        return _metadataCache.GetOrAdd(type, t =>
        {
            var tableAttr =
                t.GetCustomAttribute<TableAttribute>();

            var tableName =
                tableAttr?.Name ?? t.Name;

            var properties = t.GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToList();

            var keys = properties
                .Where(p =>
                    p.GetCustomAttribute<KeyAttribute>() != null)
                .ToHashSet();

            var identities = properties
                .Where(p =>
                    p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                        ?.DatabaseGeneratedOption
                        == DatabaseGeneratedOption.Identity)
                .ToHashSet();

            var required = properties
                .Where(p =>
                    p.GetCustomAttribute<RequiredAttribute>() != null)
                .ToHashSet();

            var columnMappings = properties.ToDictionary(
                p => p,
                p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name);

            var columnToProperty = properties.ToDictionary(
                p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name,
                p => p,
                StringComparer.OrdinalIgnoreCase
            );

            return new EntityMetadata
            {
                TableName = tableName,
                Properties = properties,
                KeyProperties = keys,
                IdentityProperties = identities,
                RequiredProperties = required,
                ColumnMappings = columnMappings,
                ColumnToProperty = columnToProperty
            };

        });
    }

    public static IEnumerable<string> GetPropertiesFromExpression<T>(
        Expression<Func<T, object>> expression)
    {
        if (expression.Body is NewExpression newExpr)
        {
            // c => new { c.Nombre, c.Email }
            return newExpr.Members.Select(m => m.Name);
        }

        if (expression.Body is MemberExpression memberExpr)
        {
            // c => c.Nombre
            return new[] { memberExpr.Member.Name };
        }

        if (expression.Body is UnaryExpression unary &&
            unary.Operand is MemberExpression innerMember)
        {
            // c => (object)c.Nombre
            return new[] { innerMember.Member.Name };
        }

        throw new ArgumentException(
            "Expresión no soportada. Usa c => new { c.Prop1, c.Prop2 } o c => c.Prop");
    }

    public static string GetTableName<T>()
        where T : class
        => GetMetadata<T>().TableName;

    public static string GetColumnName(
        PropertyInfo prop,
        Type entityType)
    {
        return GetMetadata(entityType)
            .ColumnMappings
            .TryGetValue(prop, out var name)
                ? name
                : prop.Name;
    }

    public static bool IsKey(
        PropertyInfo prop,
        Type entityType)
    {
        return GetMetadata(entityType)
            .KeyProperties
            .Contains(prop);
    }

    public static bool IsIdentity(
        PropertyInfo prop,
        Type entityType)
    {
        return GetMetadata(entityType)
            .IdentityProperties
            .Contains(prop);
    }

    public static bool IsRequired(
        PropertyInfo prop,
        Type entityType)
    {
        return GetMetadata(entityType)
            .RequiredProperties
            .Contains(prop);
    }

    public static bool IsNotMapped(
        PropertyInfo prop)
    {
        return prop.GetCustomAttribute<NotMappedAttribute>() != null;
    }
}
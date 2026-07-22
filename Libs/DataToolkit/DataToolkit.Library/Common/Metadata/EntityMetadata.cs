using System.Reflection;

namespace DataToolkit.Library.Common.Metadata;

internal sealed class EntityMetadata
{
    public string TableName { get; set; }
    public List<PropertyInfo> Properties { get; set; } = new();
    public HashSet<PropertyInfo> KeyProperties { get; set; } = new();
    public HashSet<PropertyInfo> IdentityProperties { get; set; } = new();
    public HashSet<PropertyInfo> RequiredProperties { get; set; } = new();
    public Dictionary<PropertyInfo, string> ColumnMappings { get; set; } = new();
    public Dictionary<string, PropertyInfo> ColumnToProperty { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
}
namespace DataToolkit.Library;

public class ColumnMetadata
{
    public string Name { get; set; } = "";
    public string SqlType { get; set; } = "";
    public string? MaxLength { get; set; }
    public string? Precision { get; set; }
    public string? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public string? Collation { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? PrimaryKeyName { get; set; }
    public string? ForeignTable { get; set; }
    public string? ForeignColumn { get; set; }
    public string? ForeignKeyName { get; set; }
    public string? FK_DeleteAction { get; set; }
    public string? FK_UpdateAction { get; set; }
    public bool FK_IsDisabled { get; set; }
    public bool FK_IsNotTrusted { get; set; }
}
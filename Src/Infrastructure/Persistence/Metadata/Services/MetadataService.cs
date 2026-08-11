using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Persistence.Metadata.Queries;

namespace Persistence.Metadata.Services;

public sealed class MetadataService
{
    private readonly IDatabaseContext _database;

    public MetadataService(
        IDatabaseContext database
        )
    {
        _database = database;
    }

    public async Task<List<TableMetadata>> ExtractMetadataAsync(
        //bool isSource,
        string database,
        string? schema = null,
        List<string>? tables = null)
    {

        //using IUnitOfWork unitOfWork = isSource? _database["Source"].CreateNew(): _database["Target"].CreateNew();
        using IUnitOfWork unitOfWork = _database[database].CreateNew();

        var rows = await MetadataQueries.GetMetadataAsync(
            unitOfWork,
            schema,
            tables);

        Dictionary<string, TableMetadata> metadata = new();

        foreach (var reader in rows)
        {
            string schemaName = reader["SchemaName"]?.ToString() ?? string.Empty;
            string tableName = reader["TableName"]?.ToString() ?? string.Empty;

            string key = $"{schemaName}.{tableName}";

            if (!metadata.TryGetValue(key, out TableMetadata? table))
            {
                table = new TableMetadata
                {
                    Schema = schemaName,
                    Name = tableName,
                    Columns = new List<ColumnMetadata>()
                };

                metadata.Add(key, table);
            }

            table.Columns.Add(new ColumnMetadata
            {
                Name = reader["ColumnName"]?.ToString() ?? string.Empty,
                SqlType = reader["DataType"]?.ToString() ?? string.Empty,
                MaxLength = reader["MaxLength"]?.ToString(),
                Precision = reader["Precision"]?.ToString(),
                Scale = reader["Scale"]?.ToString(),

                IsNullable = string.Equals(
                    reader["IsNullable"]?.ToString(),
                    "YES",
                    StringComparison.OrdinalIgnoreCase),

                IsIdentity = string.Equals(
                    reader["IsIdentity"]?.ToString(),
                    "YES",
                    StringComparison.OrdinalIgnoreCase),

                IsComputed = string.Equals(
                    reader["IsComputed"]?.ToString(),
                    "YES",
                    StringComparison.OrdinalIgnoreCase),

                Collation = reader["Collation"]?.ToString(),
                DefaultValue = reader["DefaultValue"]?.ToString(),

                IsPrimaryKey = string.Equals(
                    reader["IsPrimaryKey"]?.ToString(),
                    "YES",
                    StringComparison.OrdinalIgnoreCase),

                PrimaryKeyName = reader["PrimaryKeyName"]?.ToString(),
                ForeignTable = reader["ForeignTable"]?.ToString(),
                ForeignColumn = reader["ForeignColumn"]?.ToString(),
                ForeignKeyName = reader["ForeignKeyName"]?.ToString(),
                FK_DeleteAction = reader["FK_DeleteAction"]?.ToString(),
                FK_UpdateAction = reader["FK_UpdateAction"]?.ToString(),
                FK_IsDisabled = reader["FK_IsDisabled"]?.ToString() == "1",
                FK_IsNotTrusted = reader["FK_IsNotTrusted"]?.ToString() == "1",
                RecordCount = reader["RecordCount"] is DBNull? 0 : Convert.ToInt64(reader["RecordCount"])

            });
        }

        return metadata.Values.ToList();
    }
}

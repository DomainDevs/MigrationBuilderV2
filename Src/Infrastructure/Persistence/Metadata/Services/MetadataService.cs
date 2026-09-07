using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.Configuration;
using Persistence.Metadata.Queries;

namespace Persistence.Metadata.Services;

public sealed class MetadataService
{
    private const int DefaultCommandTimeout = 600;
    private const int MetadataBatchSize = 500;

    private readonly IDatabaseContext _database;
    private readonly IConfiguration _configuration;

    public MetadataService(
        IDatabaseContext database,
        IConfiguration configuration)
    {
        _database = database;
        _configuration = configuration;
    }

    public async Task<List<TableMetadata>> ExtractMetadataAsync(
        string database,
        string? schema = null,
        List<string>? tables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        using IUnitOfWork unitOfWork =
            _database[database].CreateNew();

        int commandTimeout =
            _configuration.GetValue<int>(
                $"Connections:{database}:TimeOut");

        if (commandTimeout <= 0)
            commandTimeout = DefaultCommandTimeout;

        /*
         * Cuando no se especifican tablas, MetadataQueries obtiene
         * directamente todo el metadata del schema mediante @Schema.
         *
         * Cuando se especifican tablas, no se envía la lista completa
         * a SQL Server. Se divide en bloques de 500 tablas para evitar
         * superar el límite de 2100 parámetros de SQL Server.
         *
         * El resultado de cada bloque se acumula en el mismo diccionario.
         */
        Dictionary<string, TableMetadata> metadata =
            new(StringComparer.OrdinalIgnoreCase);

        if (tables is null || tables.Count == 0)
        {
            IEnumerable<IDictionary<string, object>> rows =
                await MetadataQueries.GetMetadataAsync(
                    unitOfWork,
                    schema,
                    null,
                    commandTimeout);

            AddMetadataRows(
                metadata,
                rows);
        }
        else
        {
            List<string> requestedTables =
                tables
                    .Where(
                        table =>
                            !string.IsNullOrWhiteSpace(table))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            for (
                int offset = 0;
                offset < requestedTables.Count;
                offset += MetadataBatchSize)
            {
                int count =
                    Math.Min(
                        MetadataBatchSize,
                        requestedTables.Count - offset);

                List<string> batch =
                    requestedTables.GetRange(
                        offset,
                        count);

                IEnumerable<IDictionary<string, object>> rows =
                    await MetadataQueries.GetMetadataAsync(
                        unitOfWork,
                        schema,
                        batch,
                        commandTimeout);

                AddMetadataRows(
                    metadata,
                    rows);
            }
        }

        return metadata.Values.ToList();
    }

    private static void AddMetadataRows(
        Dictionary<string, TableMetadata> metadata,
        IEnumerable<IDictionary<string, object>> rows)
    {
        foreach (IDictionary<string, object> row in rows)
        {
            string schemaName =
                GetString(
                    row,
                    "SchemaName");

            string tableName =
                GetString(
                    row,
                    "TableName");

            string key =
                $"{schemaName}.{tableName}";

            if (!metadata.TryGetValue(
                    key,
                    out TableMetadata? table))
            {
                table = new TableMetadata
                {
                    Schema = schemaName,
                    Name = tableName,
                    Columns = []
                };

                metadata.Add(
                    key,
                    table);
            }

            table.Columns.Add(
                new ColumnMetadata
                {
                    Name = GetString(
                        row,
                        "ColumnName"),

                    SqlType = GetString(
                        row,
                        "DataType"),

                    BaseSqlType = GetString(
                        row,
                        "BaseDataType"),

                    MaxLength = GetNullableString(
                        row,
                        "MaxLength"),

                    Precision = GetNullableString(
                        row,
                        "Precision"),

                    Scale = GetNullableString(
                        row,
                        "Scale"),

                    IsNullable = IsYes(
                        row,
                        "IsNullable"),

                    IsIdentity = IsYes(
                        row,
                        "IsIdentity"),

                    IsComputed = IsYes(
                        row,
                        "IsComputed"),

                    Collation = GetNullableString(
                        row,
                        "Collation"),

                    DefaultValue = GetNullableString(
                        row,
                        "DefaultValue"),

                    IsPrimaryKey = IsYes(
                        row,
                        "IsPrimaryKey"),

                    PrimaryKeyName = GetNullableString(
                        row,
                        "PrimaryKeyName"),

                    ForeignTable = GetNullableString(
                        row,
                        "ForeignTable"),

                    ForeignColumn = GetNullableString(
                        row,
                        "ForeignColumn"),

                    ForeignKeyName = GetNullableString(
                        row,
                        "ForeignKeyName"),

                    FK_DeleteAction = GetNullableString(
                        row,
                        "FK_DeleteAction"),

                    FK_UpdateAction = GetNullableString(
                        row,
                        "FK_UpdateAction"),

                    FK_IsDisabled = IsOne(
                        row,
                        "FK_IsDisabled"),

                    FK_IsNotTrusted = IsOne(
                        row,
                        "FK_IsNotTrusted"),

                    RecordCount = GetInt64(
                        row,
                        "RecordCount")
                });
        }
    }

    private static string GetString(
        IDictionary<string, object> row,
        string column)
    {
        object? value =
            row[column];

        return value is null or DBNull
            ? string.Empty
            : value.ToString() ?? string.Empty;
    }

    private static string? GetNullableString(
        IDictionary<string, object> row,
        string column)
    {
        object? value =
            row[column];

        return value is null or DBNull
            ? null
            : value.ToString();
    }

    private static bool IsYes(
        IDictionary<string, object> row,
        string column)
    {
        object? value =
            row[column];

        return value is string text &&
               text.Equals(
                   "YES",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOne(
        IDictionary<string, object> row,
        string column)
    {
        object? value =
            row[column];

        return value switch
        {
            byte number => number == 1,
            short number => number == 1,
            int number => number == 1,
            long number => number == 1,
            _ => string.Equals(
                value?.ToString(),
                "1",
                StringComparison.Ordinal)
        };
    }

    private static long GetInt64(
        IDictionary<string, object> row,
        string column)
    {
        object? value =
            row[column];

        return value is null or DBNull
            ? 0L
            : Convert.ToInt64(value);
    }
}
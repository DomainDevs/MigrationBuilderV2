using DataToolkit.Library.UnitOfWorkLayer;

namespace Persistence.Metadata.Queries;

internal static class MetadataQueries
{
    public static Task<IEnumerable<IDictionary<string, object>>> GetMetadataAsync(
        IUnitOfWork unitOfWork,
        string? schema = null,
        List<string>? tables = null, 
        int iCommandTimeout = 300)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        List<string> filters = [];
        List<string> recordCountFilters =
        [
            "p.index_id IN (0, 1)"
        ];

        Dictionary<string, object> parameters = [];

        if (!string.IsNullOrWhiteSpace(schema))
        {
            filters.Add("s.name = @Schema");
            recordCountFilters.Add("ps.name = @Schema");

            parameters["Schema"] = schema;
        }

        if (tables is { Count: > 0 })
        {
            List<string> tableParameters =
                new(tables.Count);

            for (int i = 0; i < tables.Count; i++)
            {
                string parameterName =
                    $"Table{i}";

                tableParameters.Add(
                    $"@{parameterName}");

                parameters[parameterName] =
                    tables[i];
            }

            string tableFilter =
                $"t.name IN ({string.Join(", ", tableParameters)})";

            filters.Add(tableFilter);

            recordCountFilters.Add(
                $"pt.name IN ({string.Join(", ", tableParameters)})");
        }

        string whereClause =
            filters.Count > 0
                ? $"WHERE {string.Join(" AND ", filters)}"
                : string.Empty;

        string recordCountWhereClause =
            $"WHERE {string.Join(" AND ", recordCountFilters)}";

        string sql = $"""
            SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                ty.name AS DataType,
                bt.name AS BaseDataType,

                CASE
                    WHEN c.max_length = -1 THEN 'MAX'
                    WHEN ty.name IN ('nvarchar', 'nchar')
                        THEN CAST(c.max_length / 2 AS VARCHAR(10))
                    ELSE CAST(c.max_length AS VARCHAR(10))
                END AS MaxLength,

                c.precision AS Precision,
                c.scale AS Scale,

                CASE
                    WHEN c.is_nullable = 1 THEN 'YES'
                    ELSE 'NO'
                END AS IsNullable,

                CASE
                    WHEN c.is_identity = 1 THEN 'YES'
                    ELSE 'NO'
                END AS IsIdentity,

                CASE
                    WHEN c.is_computed = 1 THEN 'YES'
                    ELSE 'NO'
                END AS IsComputed,

                c.collation_name AS Collation,

                dc.definition AS DefaultValue,

                CASE
                    WHEN pk.column_id IS NOT NULL THEN 'YES'
                    ELSE 'NO'
                END AS IsPrimaryKey,

                pk.constraint_name AS PrimaryKeyName,

                rt.name AS ForeignTable,
                rc.name AS ForeignColumn,

                fkref.name AS ForeignKeyName,

                fkref.delete_referential_action_desc AS FK_DeleteAction,
                fkref.update_referential_action_desc AS FK_UpdateAction,

                fkref.is_disabled AS FK_IsDisabled,
                fkref.is_not_trusted AS FK_IsNotTrusted,

                ISNULL(ps.RecordCount, 0) AS RecordCount

            FROM sys.tables t

            INNER JOIN sys.schemas s
                ON s.schema_id = t.schema_id

            INNER JOIN sys.columns c
                ON c.object_id = t.object_id

            INNER JOIN sys.types ty
                ON c.user_type_id = ty.user_type_id

            INNER JOIN sys.types bt
                ON c.system_type_id = bt.system_type_id
                AND bt.user_type_id = bt.system_type_id

            LEFT JOIN sys.default_constraints dc
                ON c.default_object_id = dc.object_id

            LEFT JOIN
            (
                SELECT
                    ic.object_id,
                    ic.column_id,
                    i.name AS constraint_name
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic
                    ON i.object_id = ic.object_id
                    AND i.index_id = ic.index_id
                WHERE i.is_primary_key = 1
            ) pk
                ON pk.object_id = c.object_id
                AND pk.column_id = c.column_id

            LEFT JOIN sys.foreign_key_columns fk
                ON fk.parent_object_id = c.object_id
                AND fk.parent_column_id = c.column_id

            LEFT JOIN sys.tables rt
                ON fk.referenced_object_id = rt.object_id

            LEFT JOIN sys.columns rc
                ON fk.referenced_object_id = rc.object_id
                AND fk.referenced_column_id = rc.column_id

            LEFT JOIN sys.foreign_keys fkref
                ON fk.constraint_object_id = fkref.object_id

            LEFT JOIN
            (
                SELECT
                    p.object_id,
                    SUM(p.row_count) AS RecordCount
                FROM sys.dm_db_partition_stats p

                INNER JOIN sys.tables pt
                    ON pt.object_id = p.object_id

                INNER JOIN sys.schemas ps
                    ON ps.schema_id = pt.schema_id

                {recordCountWhereClause}

                GROUP BY
                    p.object_id
            ) ps
                ON ps.object_id = t.object_id

            {whereClause}

            ORDER BY
                ISNULL(ps.RecordCount, 0),
                s.name,
                t.object_id,
                c.column_id;
            """;

        return unitOfWork.Sql.FromSqlDictionaryAsync(
            sql,
            parameters.Count == 0
                ? null
                : parameters,
            commandTimeout: iCommandTimeout);
    }
}
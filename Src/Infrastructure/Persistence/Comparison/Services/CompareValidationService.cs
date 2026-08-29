using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Migration.Metadata;
using Shared.Options;
using System.Diagnostics;

namespace Persistence.Comparison.Services;

public sealed class CompareValidationService : ICompareValidationService
{
    private const long LargeTableRecordCountThreshold = 10_000_000;
    private const int ComparisonBatchSize = 100;
    private const int MaxConcurrentBatches = 2;
    private const int DefaultCommandTimeout = 1600;
    private int iCommandTimeoutSrc = 0;
    private int iCommandTimeoutTrg = 0;

    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseContext _database;

    public CompareValidationService(
        MetadataService metadataService,
        IOptions<MigrationOptions> options,
        IConfiguration configuration,
        IDatabaseContext database)
    {
        _metadataService = metadataService;
        _options = options.Value;
        _configuration = configuration;
        _database = database;
    }

    public async Task<CompareValidationResponseDto> CompareValidationAsync(
        CompareValidationCommand command)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string strSource =
            _configuration[$"Connections:{command.Source}:Database"]
            ?? throw new InvalidOperationException(
                $"No se encontró la configuración Connections:{command.Source}:Database.");

        string strTarget =
            _configuration[$"Connections:{command.Target}:Database"]
            ?? throw new InvalidOperationException(
                $"No se encontró la configuración Connections:{command.Target}:Database.");

        string strSourceServer =
            _configuration[$"Connections:{command.Source}:Server"]
            ?? throw new InvalidOperationException(
                $"No se encontró la configuración Connections:{command.Source}:Server.");

        string strTargetServer =
            _configuration[$"Connections:{command.Target}:Server"]
            ?? throw new InvalidOperationException(
                $"No se encontró la configuración Connections:{command.Target}:Server.");

        iCommandTimeoutSrc =
            _configuration.GetValue<int>($"Connections:{command.Source}:TimeOut");
        iCommandTimeoutTrg =
            _configuration.GetValue<int>($"Connections:{command.Target}:TimeOut");

        if (iCommandTimeoutSrc <= 0)
            iCommandTimeoutSrc = DefaultCommandTimeout;
        if (iCommandTimeoutTrg <= 0)
            iCommandTimeoutTrg = DefaultCommandTimeout;

        if (!string.Equals(
                strSourceServer,
                strTargetServer,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La comparación de validación solo se puede ejecutar entre conexiones del mismo servidor SQL.");
        }

        List<string> differences = [];
        List<string> warnings = [];
        List<SkippedTableDto> skippedTables = [];

        Task<List<TableMetadata>> sourceTask =
            _metadataService.ExtractMetadataAsync(
                command.Source,
                command.Schema,
                command.Tables);

        Task<List<TableMetadata>> targetTask =
            _metadataService.ExtractMetadataAsync(
                command.Target,
                command.Schema,
                command.Tables);

        await Task.WhenAll(
            sourceTask,
            targetTask);

        List<TableMetadata> sourceMetadata =
            MetadataNormalizer.NormalizeColumns(
                sourceTask.Result);

        List<TableMetadata> targetMetadata =
            MetadataNormalizer.NormalizeColumns(
                targetTask.Result);

        Dictionary<string, TableMetadata> sourceTables =
            sourceMetadata.ToDictionary(
                table => $"{table.Schema}.{table.Name}",
                StringComparer.OrdinalIgnoreCase);

        Dictionary<string, TableMetadata> targetTables =
            targetMetadata.ToDictionary(
                table => $"{table.Schema}.{table.Name}",
                StringComparer.OrdinalIgnoreCase);

        foreach (string table in command.Tables)
        {
            string tableKey =
                $"{command.Schema}.{table}";

            if (!sourceTables.ContainsKey(tableKey))
            {
                warnings.Add(
                    $"La tabla solicitada '{tableKey}' " +
                    "no existe en la base de datos Source.");
            }

            if (!targetTables.ContainsKey(tableKey))
            {
                warnings.Add(
                    $"La tabla solicitada '{tableKey}' " +
                    "no existe en la base de datos Target.");
            }
        }

        List<TableComparisonWorkItem> comparisonTables = [];

        int validatedCount = 0;

        foreach (TableMetadata targetTable in targetMetadata)
        {
            string tableKey =
                $"{targetTable.Schema}.{targetTable.Name}";

            if (!sourceTables.TryGetValue(
                    tableKey,
                    out TableMetadata? sourceTable))
            {
                warnings.Add(
                    $"La tabla '{tableKey}' no existe en la base de datos Source.");

                continue;
            }

            if (!MetadataMatches(
                    sourceTable,
                    targetTable))
            {
                warnings.Add(
                    $"El metadata de la tabla '{tableKey}' " +
                    "no coincide entre Source y Target.");

                continue;
            }

            if (HasNonComparableColumns(
                    sourceTable,
                    targetTable))
            {
                warnings.Add(
                    $"La tabla '{tableKey}' no se pudo comparar " +
                    "porque contiene columnas no comparables de tipo [IMAGE]");

                continue;
            }

            long sourceRecordCount =
                GetRecordCount(sourceTable);

            long targetRecordCount =
                GetRecordCount(targetTable);

            /*
             * Las tablas grandes no participan en la comparación
             * normal. Se registran explícitamente como omitidas.
             *
             * Se evalúan ambos lados porque cualquiera de las dos
             * bases puede superar el límite.
             */
            if (sourceRecordCount > LargeTableRecordCountThreshold ||
                targetRecordCount > LargeTableRecordCountThreshold)
            {
                skippedTables.Add(
                    new SkippedTableDto
                    {
                        Table = tableKey,
                        SourceRecords = sourceRecordCount,
                        TargetRecords = targetRecordCount,
                        Reason = "TABLE_TOO_LARGE",
                        Message =
                            $"La tabla supera el límite de " +
                            $"{LargeTableRecordCountThreshold:N0} registros. " +
                            "Debe ejecutarse una validación independiente."
                    });

                continue;
            }

            /*
             * A partir de aquí la tabla sí fue validada.
             *
             * El conteo ya forma parte del metadata.
             */
            validatedCount++;

            /*
             * Si la cantidad de registros es diferente,
             * no necesitamos leer los datos.
             */
            if (sourceRecordCount != targetRecordCount)
            {
                differences.Add(tableKey);
                continue;
            }

            /*
             * Ambas tablas están vacías.
             * Ya fueron validadas por metadata y conteo.
             */
            if (sourceRecordCount == 0)
            {
                continue;
            }

            comparisonTables.Add(
                new TableComparisonWorkItem(
                    sourceTable,
                    targetTable,
                    sourceRecordCount));
        }

        if (comparisonTables.Count > 0)
        {
            List<List<TableComparisonWorkItem>> batches =
                CreateBatches(
                    comparisonTables,
                    ComparisonBatchSize);

            using SemaphoreSlim semaphore =
                new(MaxConcurrentBatches);

            List<Task<List<string>>> tasks =
                new(batches.Count);

            foreach (List<TableComparisonWorkItem> batch in batches)
            {
                tasks.Add(
                    CompareBatchAsync(
                        batch,
                        command.Target,
                        strSource,
                        strTarget,
                        semaphore));
            }

            List<string>[] batchResults =
                await Task.WhenAll(tasks);

            foreach (List<string> batchDifferences in batchResults)
            {
                differences.AddRange(
                    batchDifferences);
            }
        }

        differences.Sort(
            StringComparer.OrdinalIgnoreCase);

        skippedTables.Sort(
            static (x, y) =>
                y.SourceRecords
                    .CompareTo(x.SourceRecords));

        stopwatch.Stop();

        return new CompareValidationResponseDto
        {
            Source = strSource,
            Target = strTarget,
            Tables = targetMetadata.Count,
            Validated = validatedCount,
            WithDifferences = differences.Count,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            ElapsedTime = stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"),
            Differences = differences,
            Warnings = warnings,
            SkippedTables = skippedTables
        };
    }

    private async Task<List<string>> CompareBatchAsync(
        List<TableComparisonWorkItem> tables,
        string targetConnection,
        string sourceDatabase,
        string targetDatabase,
        SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();

        try
        {
            string sql =
                BuildComparisonSql(
                    tables,
                    sourceDatabase,
                    targetDatabase);

            /*
             * targetConnection es el alias configurado,
             * no necesariamente el nombre físico de la BD.
             */
            using var target =
                _database[targetConnection].CreateNew();

            IEnumerable<IDictionary<string, object>> rows =
                await target.Sql.FromSqlDictionaryAsync(
                    sql,
                    commandTimeout: iCommandTimeoutTrg); //iCommandTimeoutSrc o iCommandTimeoutTrg

            List<string> differences = [];

            foreach (IDictionary<string, object> row in rows)
            {
                string tableKey =
                    row["TableName"]?.ToString()
                    ?? string.Empty;

                int hasDifferences =
                    row["HasDifferences"] is DBNull
                        ? 0
                        : Convert.ToInt32(
                            row["HasDifferences"]);

                if (hasDifferences == 1)
                {
                    differences.Add(tableKey);
                }
            }

            return differences;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string BuildComparisonSql(
        List<TableComparisonWorkItem> tables,
        string sourceDatabase,
        string targetDatabase)
    {
        List<string> queries =
            new(tables.Count);

        foreach (TableComparisonWorkItem workItem in tables)
        {
            string query;

            bool hasPrimaryKey =
                HasPrimaryKey(
                    workItem.SourceTable);

            bool samePrimaryKey =
                HasSamePrimaryKey(
                    workItem.SourceTable,
                    workItem.TargetTable);

            /*
             * Las tablas grandes ya fueron filtradas antes
             * de llegar aquí.
             *
             * Por seguridad mantenemos la estrategia especial
             * para tablas que eventualmente superen el límite.
             */
            bool isLargeTable =
                workItem.RecordCount >
                LargeTableRecordCountThreshold;

            if (isLargeTable &&
                hasPrimaryKey &&
                samePrimaryKey)
            {
                query =
                    BuildLargeTableHashComparisonSql(
                        workItem,
                        sourceDatabase,
                        targetDatabase);
            }
            else
            {
                query =
                    BuildExceptComparisonSql(
                        workItem,
                        sourceDatabase,
                        targetDatabase);
            }

            queries.Add(query);
        }

        return string.Join(
            Environment.NewLine +
            "UNION ALL" +
            Environment.NewLine,
            queries);
    }

    private static string BuildExceptComparisonSql(
        TableComparisonWorkItem workItem,
        string sourceDatabase,
        string targetDatabase)
    {
        TableMetadata table =
            workItem.TargetTable;

        string tableName =
            $"[{table.Schema}].[{table.Name}]";

        /*
        string columnList =
            string.Join(
                ", ",
                table.Columns.Select(
                    column => $"[{column.Name}]"));
        */
        string columnList =
            string.Join(
                ", ",
                table.Columns
                    .Where(column => !IsExcludedComparisonColumn(column))
                    .Select(column => $"[{column.Name}]"));

        string tableKey =
            EscapeSqlLiteral(
                $"{table.Schema}.{table.Name}");

        return $$"""
        SELECT
            N'{{tableKey}}' AS TableName,
            CASE
                WHEN EXISTS
                (
                    SELECT {{columnList}}
                    FROM [{{sourceDatabase}}].{{tableName}}

                    EXCEPT

                    SELECT {{columnList}}
                    FROM [{{targetDatabase}}].{{tableName}}
                )
                OR EXISTS
                (
                    SELECT {{columnList}}
                    FROM [{{targetDatabase}}].{{tableName}}

                    EXCEPT

                    SELECT {{columnList}}
                    FROM [{{sourceDatabase}}].{{tableName}}
                )
                THEN 1
                ELSE 0
            END AS HasDifferences
        """;
    }

    private static string BuildLargeTableHashComparisonSql(
        TableComparisonWorkItem workItem,
        string sourceDatabase,
        string targetDatabase)
    {
        TableMetadata sourceTable =
            workItem.SourceTable;

        TableMetadata targetTable =
            workItem.TargetTable;

        List<ColumnMetadata> sourcePrimaryKeys =
            sourceTable.Columns
                .Where(column => column.IsPrimaryKey)
                .ToList();

        List<ColumnMetadata> targetPrimaryKeys =
            targetTable.Columns
                .Where(column => column.IsPrimaryKey)
                .ToList();

        string sourceTableName =
            $"[{sourceTable.Schema}].[{sourceTable.Name}]";

        string targetTableName =
            $"[{targetTable.Schema}].[{targetTable.Name}]";

        string joinCondition =
            string.Join(
                Environment.NewLine +
                "                AND ",
                sourcePrimaryKeys.Select(
                    sourceColumn =>
                    {
                        ColumnMetadata targetColumn =
                            targetPrimaryKeys.First(
                                column =>
                                    column.Name.Equals(
                                        sourceColumn.Name,
                                        StringComparison.OrdinalIgnoreCase));

                        return
                            $"S.[{sourceColumn.Name}] = T.[{targetColumn.Name}]";
                    }));

        string sourceHashExpression =
            BuildHashExpression(
                sourceTable);

        string targetHashExpression =
            BuildHashExpression(
                targetTable);

        string sourcePrimaryKeyNull =
            BuildPrimaryKeyNullCondition(
                sourcePrimaryKeys,
                "S");

        string targetPrimaryKeyNull =
            BuildPrimaryKeyNullCondition(
                targetPrimaryKeys,
                "T");

        string tableKey =
            EscapeSqlLiteral(
                $"{targetTable.Schema}.{targetTable.Name}");

        return $$"""
        SELECT
            N'{{tableKey}}' AS TableName,
            CASE
                WHEN EXISTS
                (
                    SELECT TOP (1)
                        1
                    FROM
                    (
                        SELECT
                            S.*,
                            {{sourceHashExpression}} AS __RowHash
                        FROM [{{sourceDatabase}}].{{sourceTableName}} AS S
                    ) AS S
                    FULL OUTER JOIN
                    (
                        SELECT
                            T.*,
                            {{targetHashExpression}} AS __RowHash
                        FROM [{{targetDatabase}}].{{targetTableName}} AS T
                    ) AS T
                        ON {{joinCondition}}
                    WHERE
                        {{sourcePrimaryKeyNull}}
                        OR {{targetPrimaryKeyNull}}
                        OR S.__RowHash <> T.__RowHash
                )
                THEN 1
                ELSE 0
            END AS HasDifferences
        """;
    }

    private static string BuildHashExpression(
        TableMetadata table)
    {
        IEnumerable<string> expressions =
            table.Columns
                .Where(
                    column =>
                        !column.IsPrimaryKey &&
                        !IsNonComparable(column))
                .Select(
                    column =>
                        $"ISNULL(CONVERT(NVARCHAR(MAX), [{column.Name}]), N'<NULL>')");

        string concatenated =
            string.Join(
                " + N'|' + ",
                expressions);

        if (string.IsNullOrWhiteSpace(concatenated))
        {
            concatenated = "N''";
        }

        return
            $"HASHBYTES('SHA2_256', {concatenated})";
    }

    private static string BuildPrimaryKeyNullCondition(
        List<ColumnMetadata> primaryKeys,
        string alias)
    {
        if (primaryKeys.Count == 1)
        {
            return
                $"{alias}.[{primaryKeys[0].Name}] IS NULL";
        }

        return string.Join(
            " AND ",
            primaryKeys.Select(
                column =>
                    $"{alias}.[{column.Name}] IS NULL"));
    }

    private static bool HasPrimaryKey(
        TableMetadata table)
    {
        return table.Columns.Any(
            column => column.IsPrimaryKey);
    }

    private static bool HasSamePrimaryKey(
        TableMetadata sourceTable,
        TableMetadata targetTable)
    {
        List<string> sourceKeys =
            sourceTable.Columns
                .Where(column => column.IsPrimaryKey)
                .Select(column => column.Name)
                .ToList();

        List<string> targetKeys =
            targetTable.Columns
                .Where(column => column.IsPrimaryKey)
                .Select(column => column.Name)
                .ToList();

        if (sourceKeys.Count !=
            targetKeys.Count)
        {
            return false;
        }

        return sourceKeys.SequenceEqual(
            targetKeys,
            StringComparer.OrdinalIgnoreCase);
    }

    private static long GetRecordCount(
        TableMetadata table)
    {
        if (table.Columns.Count == 0)
        {
            return 0;
        }

        return table.Columns[0].RecordCount;
    }

    private static List<List<TableComparisonWorkItem>> CreateBatches(
        List<TableComparisonWorkItem> tables,
        int batchSize)
    {
        List<List<TableComparisonWorkItem>> batches = [];

        for (int index = 0;
             index < tables.Count;
             index += batchSize)
        {
            batches.Add(
                tables
                    .Skip(index)
                    .Take(batchSize)
                    .ToList());
        }

        return batches;
    }

    private static bool MetadataMatches(
        TableMetadata sourceTable,
        TableMetadata targetTable)
    {
        if (!sourceTable.Schema.Equals(
                targetTable.Schema,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!sourceTable.Name.Equals(
                targetTable.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (sourceTable.Columns.Count !=
            targetTable.Columns.Count)
        {
            return false;
        }

        HashSet<string> sourceColumns =
            sourceTable.Columns
                .Select(column => column.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        HashSet<string> targetColumns =
            targetTable.Columns
                .Select(column => column.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        return sourceColumns.SetEquals(
            targetColumns);
    }

    private static bool IsExcludedComparisonColumn(
        ColumnMetadata column)
    {
        return column.SqlType.Equals(
                   "image",
                   StringComparison.OrdinalIgnoreCase)
               || column.SqlType.Equals(
                   "text",
                   StringComparison.OrdinalIgnoreCase)
               || column.SqlType.Equals(
                   "ntext",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasNonComparableColumns(
        TableMetadata sourceTable,
        TableMetadata targetTable)
    {
        return sourceTable.Columns.Any(
                   IsNonComparable)
               || targetTable.Columns.Any(
                   IsNonComparable);
    }

    private static bool IsNonComparable(
        ColumnMetadata column)
    {
        return column.SqlType.Equals(
                   "image",
                   StringComparison.OrdinalIgnoreCase)
               || column.SqlType.Equals(
                   "text",
                   StringComparison.OrdinalIgnoreCase)
               || column.SqlType.Equals(
                   "ntext",
                   StringComparison.OrdinalIgnoreCase)
               || column.SqlType.Equals(
                   "xml",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeSqlLiteral(
        string value)
    {
        return value.Replace(
            "'",
            "''",
            StringComparison.Ordinal);
    }

    private sealed record TableComparisonWorkItem(
        TableMetadata SourceTable,
        TableMetadata TargetTable,
        long RecordCount);
}
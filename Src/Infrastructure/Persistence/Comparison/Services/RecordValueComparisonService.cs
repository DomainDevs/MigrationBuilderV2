using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Migration.Metadata;
using Persistence.Migration.Services;
using Shared.Options;
using System.Diagnostics;
using System.Text.Json;

namespace Persistence.Comparison.Services;


public sealed class RecordValueComparisonService
    : IRecordValueComparisonService
{
    private const int DefaultCommandTimeout = 1600;

    private static readonly HashSet<string> NumericTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "tinyint",
            "smallint",
            "int",
            "bigint",
            "decimal",
            "numeric",
            "money",
            "smallmoney",
            "float",
            "real"
        };

    private static readonly string[] ValueNameTokens =
    [
        "prima",
        "suma",
        "valor",
        "monto",
        "importe",
        "cantidad",
        "total",
        "subtotal",
        "saldo",
        "precio",
        "costo",
        "coste",
        "capital",
        "interes",
        "interés",
        "descuento",
        "recargo",
        "comision",
        "comisión",
        "porcentaje",
        "porc",
        "base",
        "iva",
        "impuesto",
        "retencion",
        "retención",
        "aporte",
        "ingreso",
        "egreso",
        "debito",
        "débito",
        "credito",
        "crédito",
        "pago",
        "pagado",
        "facturado",
        "financiado",
        "financiacion",
        "financiación"
    ];

    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseContext _database;

    public RecordValueComparisonService(
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

    public async Task<RecordValueComparisonResult> CompareAsync(
        CompareRecordValueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Source);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Target);

        Stopwatch stopwatch = Stopwatch.StartNew();

        string schema =
            string.IsNullOrWhiteSpace(command.Schema)
                ? "dbo"
                : command.Schema;

        List<string> requestedTables =
            GetRequestedTables(command);

        Task<List<TableMetadata>> sourceTask =
            _metadataService.ExtractMetadataAsync(
                command.Source,
                schema,
                requestedTables.Count > 0
                    ? requestedTables
                    : null);

        Task<List<TableMetadata>> targetTask =
            _metadataService.ExtractMetadataAsync(
                command.Target,
                schema,
                requestedTables.Count > 0
                    ? requestedTables
                    : null);

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

        List<string> tableKeys =
            BuildTableKeys(
                sourceTables,
                targetTables);

        List<TableValueComparisonResult> tableResults = [];

        int tablesMatching = 0;
        int tablesWithDifferences = 0;
        int tablesOnlyInSource = 0;
        int tablesOnlyInTarget = 0;
        int tablesWithoutValueColumns = 0;
        int valueColumnsCompared = 0;
        int valueColumnsWithDifferences = 0;

        int sourceTimeout =
            GetCommandTimeout(command.Source);

        int targetTimeout =
            GetCommandTimeout(command.Target);

        foreach (string tableKey in tableKeys)
        {
            bool sourceExists =
                sourceTables.TryGetValue(
                    tableKey,
                    out TableMetadata? sourceTable);

            bool targetExists =
                targetTables.TryGetValue(
                    tableKey,
                    out TableMetadata? targetTable);

            if (!sourceExists)
            {
                tablesOnlyInTarget++;

                tableResults.Add(
                    new TableValueComparisonResult
                    {
                        Table = tableKey,
                        Status = "TARGET_ONLY"
                    });

                continue;
            }

            if (!targetExists)
            {
                tablesOnlyInSource++;

                tableResults.Add(
                    new TableValueComparisonResult
                    {
                        Table = tableKey,
                        Status = "SOURCE_ONLY"
                    });

                continue;
            }

            List<ColumnMetadata> valueColumns =
                GetCommonValueColumns(
                    sourceTable!,
                    targetTable!);

            if (valueColumns.Count == 0)
            {
                tablesWithoutValueColumns++;

                tableResults.Add(
                    new TableValueComparisonResult
                    {
                        Table = tableKey,
                        Status = "NO_VALUE_COLUMNS"
                    });

                continue;
            }

            Dictionary<string, decimal> sourceValues =
                await GetColumnSumsAsync(
                    sourceTable!,
                    valueColumns,
                    command.Source,
                    sourceTimeout);

            Dictionary<string, decimal> targetValues =
                await GetColumnSumsAsync(
                    targetTable!,
                    valueColumns,
                    command.Target,
                    targetTimeout);

            List<ColumnValueComparisonResult> columnResults = [];

            bool tableHasDifferences = false;

            foreach (ColumnMetadata column in valueColumns)
            {
                decimal sourceValue =
                    sourceValues.TryGetValue(
                        column.Name,
                        out decimal sourceSum)
                        ? sourceSum
                        : 0m;

                decimal targetValue =
                    targetValues.TryGetValue(
                        column.Name,
                        out decimal targetSum)
                        ? targetSum
                        : 0m;

                decimal difference =
                    targetValue - sourceValue;

                bool different =
                    difference != 0m;

                if (different)
                {
                    tableHasDifferences = true;
                    valueColumnsWithDifferences++;
                }

                valueColumnsCompared++;

                columnResults.Add(
                    new ColumnValueComparisonResult
                    {
                        Column = column.Name,
                        SqlType = column.BaseSqlType,
                        SourceValue = sourceValue,
                        TargetValue = targetValue,
                        Difference = difference,
                        Status = different
                            ? "DIFFERENT"
                            : "OK"
                    });
            }

            string tableStatus =
                tableHasDifferences
                    ? "DIFFERENT"
                    : "OK";

            if (tableHasDifferences)
                tablesWithDifferences++;
            else
                tablesMatching++;

            tableResults.Add(
                new TableValueComparisonResult
                {
                    Table = tableKey,
                    Status = tableStatus,
                    Columns = columnResults
                });
        }

        stopwatch.Stop();

        return new RecordValueComparisonResult
        {
            ElapsedMilliseconds =
                stopwatch.ElapsedMilliseconds,

            Summary = new RecordValueComparisonSummary
            {
                TablesCompared = tableKeys.Count,
                TablesMatching = tablesMatching,
                TablesWithDifferences = tablesWithDifferences,
                TablesOnlyInSource = tablesOnlyInSource,
                TablesOnlyInTarget = tablesOnlyInTarget,
                TablesWithoutValueColumns = tablesWithoutValueColumns,
                ValueColumnsCompared = valueColumnsCompared,
                ValueColumnsWithDifferences =
                    valueColumnsWithDifferences
            },

            Tables = tableResults
        };
    }

    private List<string> GetRequestedTables(
        CompareRecordValueCommand command)
    {
        List<string> requestedTables =
            command.Tables?
                .Where(static table =>
                    !string.IsNullOrWhiteSpace(table))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList()
            ?? [];

        if (requestedTables.Count > 0)
            return requestedTables;

        if (!string.IsNullOrWhiteSpace(
                command.ProjectName))
        {
            return GetTablesFromMigrationPlan(
                command.ProjectName);
        }

        return [];
    }

    private List<string> GetTablesFromMigrationPlan(
        string projectName)
    {
        string projectPath =
            Path.Combine(
                _options.Folders.Root,
                projectName);

        string planPath =
            Path.Combine(
                projectPath,
                "MigrationPlan.json");

        if (!File.Exists(planPath))
        {
            throw new FileNotFoundException(
                $"No se encontró el MigrationPlan.json del proyecto '{projectName}'.",
                planPath);
        }

        string json =
            File.ReadAllText(planPath);

        MigrationPlan? plan =
            JsonSerializer.Deserialize<MigrationPlan>(
                json);

        if (plan is null)
        {
            throw new InvalidOperationException(
                $"No se pudo leer el MigrationPlan del proyecto '{projectName}'.");
        }
        /*
        return plan.Packages
            .Where(static package => package.Enabled)
            .Select(static package => package.Package)
            .Where(static package =>
                !string.IsNullOrWhiteSpace(package))
            .Select(static package =>
            {
                int separator =
                    package.IndexOf('.');

                return separator >= 0
                    ? package[(separator + 1)..]
                    : package;
            })
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
        */
        List<string> packages = plan.Packages
            .Where(static package => package.Enabled)
            .Select(static package => package.Package)
            .Where(static package =>
                !string.IsNullOrWhiteSpace(package))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Validar las carpetas usando todavía "dbo.mitabla"
        packages.RemoveAll(package =>
        {
            string packagePath =
                Path.Combine(
                    projectPath,
                    _options.Folders.MigrationTask.DirectoryName,
                    _options.Folders.MigrationTask.DataIngestion,
                    package);

            return !Directory.Exists(packagePath);
        });

        // Ahora sí quitar el esquema
        List<string> tables = packages
            .Select(static package =>
            {
                int separator =
                    package.IndexOf('.');

                return separator >= 0
                    ? package[(separator + 1)..]
                    : package;
            })
            .ToList();

        return tables;

    }

    private static List<string> BuildTableKeys(
        Dictionary<string, TableMetadata> sourceTables,
        Dictionary<string, TableMetadata> targetTables)
    {
        return sourceTables.Keys
            .Union(
                targetTables.Keys,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                static table => table,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ColumnMetadata> GetCommonValueColumns(
        TableMetadata sourceTable,
        TableMetadata targetTable)
    {
        Dictionary<string, ColumnMetadata> targetColumns =
            targetTable.Columns
                .ToDictionary(
                    column => column.Name,
                    StringComparer.OrdinalIgnoreCase);

        return sourceTable.Columns
            .Where(IsValueColumn)
            .Where(column =>
                targetColumns.TryGetValue(
                    column.Name,
                    out ColumnMetadata? targetColumn)
                && IsValueColumn(targetColumn))
            .ToList();
    }

    private static bool IsValueColumn(
        ColumnMetadata column)
    {
        if (column.IsPrimaryKey)
            return false;

        if (column.IsIdentity)
            return false;

        if (column.IsComputed)
            return false;

        string sqlType =
            string.IsNullOrWhiteSpace(column.BaseSqlType)
                ? column.SqlType
                : column.BaseSqlType;

        if (!NumericTypes.Contains(sqlType))
            return false;

        return HasValueName(
            column.Name);
    }

    private static bool HasValueName(
        string columnName)
    {
        string normalized =
            columnName
                .Trim()
                .ToLowerInvariant();

        foreach (string token in ValueNameTokens)
        {
            if (normalized.Contains(
                    token,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<Dictionary<string, decimal>> GetColumnSumsAsync(
        TableMetadata table,
        IReadOnlyCollection<ColumnMetadata> columns,
        string connectionName,
        int commandTimeout)
    {
        if (columns.Count == 0)
            return new Dictionary<string, decimal>(
                StringComparer.OrdinalIgnoreCase);

        string tableName =
            BuildSqlTableName(
                table.Schema,
                table.Name);

        List<string> expressions = [];

        foreach (ColumnMetadata column in columns)
        {
            string columnName =
                EscapeSqlIdentifier(
                    column.Name);

            string alias =
                EscapeSqlIdentifier(
                    column.Name);

            string sqlType =
                string.IsNullOrWhiteSpace(column.BaseSqlType)
                    ? column.SqlType
                    : column.BaseSqlType;

            string expression;

            if (sqlType.Equals(
                    "float",
                    StringComparison.OrdinalIgnoreCase)
                || sqlType.Equals(
                    "real",
                    StringComparison.OrdinalIgnoreCase))
            {
                expression =
                    $"SUM(CONVERT(float, {columnName}))";
            }
            else if (sqlType.Equals(
                         "money",
                         StringComparison.OrdinalIgnoreCase)
                     || sqlType.Equals(
                         "smallmoney",
                         StringComparison.OrdinalIgnoreCase))
            {
                expression =
                    $"SUM(CONVERT(decimal(38, 4), {columnName}))";
            }
            else
            {
                expression =
                    $"SUM(CONVERT(decimal(38, 18), {columnName}))";
            }

            expressions.Add(
                $"ISNULL({expression}, 0) AS {alias}");
        }

        string sql =
            $"""
            SELECT
                {string.Join(",\n                ", expressions)}
            FROM {tableName};
            """;

        using IUnitOfWork database =
            _database[connectionName].CreateNew();

        IEnumerable<IDictionary<string, object>> rows =
            await database.Sql.FromSqlDictionaryAsync(
                sql,
                commandTimeout: commandTimeout);

        IDictionary<string, object>? row =
            rows.FirstOrDefault();

        Dictionary<string, decimal> result =
            new(StringComparer.OrdinalIgnoreCase);

        if (row is null)
            return result;

        foreach (ColumnMetadata column in columns)
        {
            if (!row.TryGetValue(
                    column.Name,
                    out object? value))
            {
                continue;
            }

            result[column.Name] =
                ConvertToDecimal(value);
        }

        return result;
    }

    private int GetCommandTimeout(
        string connectionName)
    {
        int timeout =
            _configuration.GetValue<int>(
                $"Connections:{connectionName}:TimeOut");

        return timeout > 0
            ? timeout
            : DefaultCommandTimeout;
    }

    private static decimal ConvertToDecimal(object? value)
    {
        if (value is null || value == DBNull.Value)
            return 0m;

        try
        {
            return Convert.ToDecimal(
                value,
                System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            if (value is double doubleValue)
                return doubleValue > 0
                    ? decimal.MaxValue
                    : decimal.MinValue;

            if (value is float floatValue)
                return floatValue > 0
                    ? decimal.MaxValue
                    : decimal.MinValue;

            throw;
        }
    }

    private static string BuildSqlTableName(
        string schema,
        string table)
    {
        return
            $"{EscapeSqlIdentifier(schema)}." +
            EscapeSqlIdentifier(table);
    }

    private static string EscapeSqlIdentifier(
        string value)
    {
        return
            $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}


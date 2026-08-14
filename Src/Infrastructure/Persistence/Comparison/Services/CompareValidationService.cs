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

namespace Persistence.Comparison.Services;

public sealed class CompareValidationService : ICompareValidationService
{
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

        List<TableMetadata> sourceMetadata =
            await _metadataService.ExtractMetadataAsync(
                command.Source,
                command.Schema,
                command.Tables);

        sourceMetadata =
            MetadataNormalizer.NormalizeColumns(
                sourceMetadata);

        List<TableMetadata> targetMetadata =
            await _metadataService.ExtractMetadataAsync(
                command.Target,
                command.Schema,
                command.Tables);

        targetMetadata =
            MetadataNormalizer.NormalizeColumns(
                targetMetadata);

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

            bool existsInSource =
                sourceTables.ContainsKey(tableKey);

            bool existsInTarget =
                targetTables.ContainsKey(tableKey);

            if (!existsInSource)
            {
                warnings.Add(
                    $"La tabla solicitada '{tableKey}' " +
                    "no existe en la base de datos Source.");
            }

            if (!existsInTarget)
            {
                warnings.Add(
                    $"La tabla solicitada '{tableKey}' " +
                    "no existe en la base de datos Target.");
            }
        }

        using var target =
            _database[command.Target].CreateNew();

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

            if (HasNonComparableColumns(sourceTable, targetTable))
            {
                warnings.Add(
                    $"La tabla '{tableKey}' no se pudo comparar " +
                    "porque contiene columnas no comparables.");

                continue;
            }

            string columnList =
                string.Join(
                    ", ",
                    targetTable.Columns.Select(
                        column => $"[{column.Name}]"));

            string sql = $$"""
            SELECT
                CASE
                    WHEN EXISTS
                    (
                        SELECT {{columnList}}
                        FROM [{{strSource}}].[{{targetTable.Schema}}].[{{targetTable.Name}}]

                        EXCEPT

                        SELECT {{columnList}}
                        FROM [{{strTarget}}].[{{targetTable.Schema}}].[{{targetTable.Name}}]
                    )
                    OR EXISTS
                    (
                        SELECT {{columnList}}
                        FROM [{{strTarget}}].[{{targetTable.Schema}}].[{{targetTable.Name}}]

                        EXCEPT

                        SELECT {{columnList}}
                        FROM [{{strSource}}].[{{targetTable.Schema}}].[{{targetTable.Name}}]
                    )
                    THEN 1
                    ELSE 0
                END AS HasDifferences;
            """;

            int hasDifferences =
                (await target.Sql.FromSqlAsync<int>(
                    sql,
                    commandTimeout: 1000))
                .Single();

            validatedCount++;

            if (hasDifferences == 1)
            {
                differences.Add(
                    tableKey);
            }
        }

        return new CompareValidationResponseDto
        {
            Source = strSource,
            Target = strTarget,
            Tables = targetMetadata.Count,
            Validated = validatedCount,
            WithDifferences = differences.Count,
            Differences = differences,
            Warnings = warnings
        };
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
                   StringComparison.OrdinalIgnoreCase);
    }
}
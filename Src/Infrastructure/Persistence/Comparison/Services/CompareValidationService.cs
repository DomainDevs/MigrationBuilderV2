using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using Application.Features.Migration.Commands;
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

        List<TableMetadata> targetMetadata =
            await _metadataService.ExtractMetadataAsync(
                command.Target,
                command.Schema,
                command.Tables);

        targetMetadata =
            MetadataNormalizer.NormalizeColumns(
                targetMetadata);

        HashSet<string> existingTables =
            targetMetadata
                .Select(t => t.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (string table in command.Tables)
        {
            if (!existingTables.Contains(table))
            {
                warnings.Add(
                    $"La tabla solicitada '{command.Schema}.{table}' " +
                    "no existe en la base de datos Target.");
            }
        }

        using var target = _database["Target"].CreateNew();

        foreach (TableMetadata targetTable in targetMetadata)
        {
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
                (await target.Sql.FromSqlAsync<int>(sql))
                .Single();

            if (hasDifferences == 1)
            {
                differences.Add(
                    $"{targetTable.Schema}.{targetTable.Name}");
            }
        }

        return new CompareValidationResponseDto
        {
            Source = strSource,
            Target = strTarget,
            Tables = targetMetadata.Count,
            Validated = targetMetadata.Count,
            WithDifferences = differences.Count,
            Differences = differences,
            Warnings = warnings
        };
    }
}

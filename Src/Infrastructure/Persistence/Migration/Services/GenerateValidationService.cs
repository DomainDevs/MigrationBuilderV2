using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using DataToolkit.Library;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Migration.Builders;
using Persistence.Migration.Helpers;
using Persistence.Migration.Metadata;
using Shared.Options;

namespace Persistence.Migration.Services;

public sealed class GenerateValidationService : IGenerateValidationService
{
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;
    private readonly IConfiguration _configuration;

    public GenerateValidationService(
        MetadataService metadataService,
        IOptions<MigrationOptions> options,
        IConfiguration configuration)
    {
        _metadataService = metadataService;
        _options = options.Value;
        _configuration = configuration;
    }

    public async Task<MigrationResponseDto> GenerateValidationAsync(
        GenerateValidationCommand command)
    {
        string projectPath =
            Path.Combine(
                _options.Folders.Root,
                command.ProjectName);

        if (!Directory.Exists(projectPath))
        {
            throw new IOException(
                $"El proyecto '{projectPath}' no existe.");
        }

        string outputFolder =
            Path.Combine(
                projectPath,
                _options.Folders.MigrationTask.DirectoryName, 
                _options.Folders.MigrationTask.DataIngestion);

        if (!Directory.Exists(outputFolder))
        {
            throw new IOException(
                $"El directorio de tareas de migración '{outputFolder}' no existe.");
        }

        string artifactPrefix = MigrationWarningExtensions.GetArtifactPrefix(command.ArtifactType);
        //string artifactPrefix = command.ArtifactType == ArtifactType.WorkFile? "WF": "STG";

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
                "El artefacto de validación solo se puede generar entre conexiones del mismo servidor SQL.");
        }

        List<string> generatedFiles = [];
        List<string> warnings = [];

        int generatedCount = 0;
        int skippedCount = 0;

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

            if (!targetTables.ContainsKey(tableKey))
            {
                warnings.Add(
                    $"La tabla solicitada '{tableKey}' " +
                    "no existe en la base de datos Target.");

                continue;
            }

            if (!sourceTables.ContainsKey(tableKey))
            {
                warnings.Add(
                    $"La tabla solicitada '{tableKey}' " +
                    "no existe en la base de datos Source.");
            }
        }

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

            string artifactFolder =
                Path.Combine(
                    outputFolder,
                    tableKey);

            string fileName =
                $"VAL_{targetTable.Schema}.{artifactPrefix}_{targetTable.Name}.sql";

            string filePath =
                Path.Combine(
                    artifactFolder,
                    fileName);

            if (File.Exists(filePath))
            {
                skippedCount++;

                warnings.Add(
                    $"El archivo '{fileName}' ya existe.");

                continue;
            }

            Directory.CreateDirectory(
                artifactFolder);

            string sql =
                ValidationBuilder.BuildValidationScript(
                    strSource,
                    strTarget,
                    targetTable);

            await File.WriteAllTextAsync(
                filePath,
                sql);

            generatedCount++;
            generatedFiles.Add(fileName);
        }

        return new MigrationResponseDto
        {
            GeneratedFiles = generatedCount,
            SkippedTables = skippedCount,
            Files = generatedFiles,
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
}
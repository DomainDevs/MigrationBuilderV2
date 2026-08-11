using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using DataToolkit.Library;
using Domain.Enums;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Migration.Builders;
using Persistence.Migration.Metadata;
using Shared.Options;

namespace Persistence.Migration.Services;

public sealed class GenerateValidationService : IGenerateValidationService
{
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;

    public GenerateValidationService(
        MetadataService metadataService,
        IOptions<MigrationOptions> options)
    {
        _metadataService = metadataService;
        _options = options.Value;
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
                _options.Folders.MigrationTask);

        if (!Directory.Exists(outputFolder))
        {
            throw new IOException(
                $"El directorio de tareas de migración '{outputFolder}' no existe.");
        }

        string artifactPrefix =
            command.ArtifactType == ArtifactType.WorkFile
                ? "WF"
                : "STG";

        List<string> generatedFiles = [];
        List<string> warnings = [];

        int generatedCount = 0;
        int skippedCount = 0;

        List<TableMetadata> targetMetadata =
            await _metadataService.ExtractMetadataAsync(
                "Target",
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

        foreach (TableMetadata targetTable in targetMetadata)
        {
            string tableKey =
                $"{targetTable.Schema}.{targetTable.Name}";

            string artifactFolder =
                Path.Combine(
                    outputFolder,
                    tableKey);

            Directory.CreateDirectory(
                artifactFolder);

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

            string sql =
                ValidationBuilder.BuildValidationScript(
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
}
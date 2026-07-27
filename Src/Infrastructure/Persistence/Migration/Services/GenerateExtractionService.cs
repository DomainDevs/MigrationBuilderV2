using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using DataToolkit.Library;
using DataToolkit.Library.UnitOfWorkLayer;
using Domain.Enums;
using Microsoft.Extensions.Options;
using Persistence.Connect.Context;
using Persistence.Metadata.Services;
using Persistence.Migration.Builders;
using Persistence.Migration.Metadata;
using Shared.Options;

namespace Persistence.Migration.Services;

public sealed class GenerateExtractionService : IGenerateExtractionService
{
    private readonly IUnitOfWork _source;
    private readonly IUnitOfWork _target;
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;

    public GenerateExtractionService(
        SqlServerContext context,
        MetadataService metadataService,
        IOptions<MigrationOptions> options)
    {
        _source = context.Source;
        _target = context.Target;
        _metadataService = metadataService;
        _options = options.Value;
    }

    public async Task<MigrationResponseDto> GenerateExtractionAsync(
        GenerateExtractionCommand command)
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

        string prefix =
            command.ArtifactType == ArtifactType.WorkFile
                ? "WF"
                : "STG";

        List<string> generatedFiles = [];
        List<string> warnings = [];

        int generatedCount = 0;
        int skippedCount = 0;

        List<TableMetadata> sourceMetadata =
            await _metadataService.ExtractMetadataAsync(
                true,
                command.Schema,
                command.Tables);

        List<TableMetadata> targetMetadata =
            await _metadataService.ExtractMetadataAsync(
                false,
                command.Schema,
                command.Tables);

        sourceMetadata =
            MetadataNormalizer.NormalizeColumns(sourceMetadata);

        targetMetadata =
            MetadataNormalizer.NormalizeColumns(targetMetadata);

        Dictionary<string, TableMetadata> sourceLookup =
            sourceMetadata.ToDictionary(
                table => $"{table.Schema}.{table.Name}",
                StringComparer.OrdinalIgnoreCase);

        foreach (TableMetadata targetTable in targetMetadata)
        {
            string tableKey =
                $"{targetTable.Schema}.{targetTable.Name}";

            if (!sourceLookup.TryGetValue(
                    tableKey,
                    out TableMetadata? sourceTable))
            {
                skippedCount++;

                warnings.Add(
                    $"No se generó el script de extracción para '{tableKey}' porque la tabla no existe en la base de datos origen.");

                continue;
            }

            string sql =
                ExtractionBuilder.BuildExtractionScript(
                    sourceTable,
                    targetTable,
                    command.ArtifactType);

            string artifactFolder =
                Path.Combine(
                    outputFolder,
                    tableKey);

            Directory.CreateDirectory(artifactFolder);

            string fileName =
                $"SQL_{targetTable.Schema}.{prefix}_{targetTable.Name}.sql";

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
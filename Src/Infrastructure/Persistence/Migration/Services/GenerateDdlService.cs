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

public sealed class GenerateDdlService : IGenerateDdlService
{
    private readonly IUnitOfWork _source;
    private readonly IUnitOfWork _target;
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;

    public GenerateDdlService(
        SqlServerContext context,
        MetadataService metadataService,
        IOptions<MigrationOptions> options)
    {
        _source = context.Source;
        _target = context.Target;
        _metadataService = metadataService;
        _options = options.Value;
    }

    public async Task<MigrationResponseDto> GenerateDdlScriptsAsync(
        GenerateDdlCommand command)
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

        string artifactPrefix =
            command.ArtifactType == ArtifactType.WorkFile
                ? "WF"
                : "STG";

        List<string> generatedFiles = [];
        List<string> warnings = [];

        int generatedCount = 0;
        int skippedCount = 0;

        Task<List<TableMetadata>> sourceTask =
            _metadataService.ExtractMetadataAsync(
                true,
                command.Schema,
                command.Tables);

        Task<List<TableMetadata>> targetTask =
            _metadataService.ExtractMetadataAsync(
                false,
                command.Schema,
                command.Tables);

        await Task.WhenAll(sourceTask, targetTask);

        List<TableMetadata> sourceMetadata =
            MetadataNormalizer.NormalizeColumns(sourceTask.Result);

        List<TableMetadata> targetMetadata =
            MetadataNormalizer.NormalizeColumns(targetTask.Result);

        Dictionary<string, TableMetadata> targetLookup =
            targetMetadata.ToDictionary(
                t => $"{t.Schema}.{t.Name}",
                StringComparer.OrdinalIgnoreCase);

        foreach (TableMetadata sourceTable in sourceMetadata)
        {
            string tableKey =
                $"{sourceTable.Schema}.{sourceTable.Name}";

            string fileName =
                $"DDL_{sourceTable.Schema}.{artifactPrefix}_{sourceTable.Name}.sql";

            if (!targetLookup.TryGetValue(
                    tableKey,
                    out TableMetadata? targetTable))
            {
                skippedCount++;

                warnings.Add(
                    $"La tabla '{tableKey}' no existe en la base de datos destino.");

                continue;
            }

            string ddl =
                DdlBuilder.BuildCreateTable(
                    sourceTable,
                    targetTable,
                    command.ArtifactType);

            string artifactFolder =
                Path.Combine(
                    outputFolder,
                    tableKey);

            BeginEndBuilder.BuildBegin(
                outputFolder,
                artifactPrefix,
                sourceTable.Schema,
                sourceTable.Name);

            BeginEndBuilder.BuildEnd(
                outputFolder,
                artifactPrefix,
                sourceTable.Schema,
                sourceTable.Name);

            Directory.CreateDirectory(artifactFolder);

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
                ddl);

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
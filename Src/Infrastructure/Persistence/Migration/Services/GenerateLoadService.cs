using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using DataToolkit.Library;
using Domain.Enums;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Migration.Builders;
using Persistence.Migration.Helpers;
using Persistence.Migration.Metadata;
using Shared.Options;

namespace Persistence.Migration.Services;

public sealed class GenerateLoadService : IGenerateLoadService
{
    //private readonly IUnitOfWork _source;
    //private readonly IUnitOfWork _target;
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;

    public GenerateLoadService(
        //SqlServerContext context,
        MetadataService metadataService,
        IOptions<MigrationOptions> options)
    {
        //_source = context.Source;
        //_target = context.Target;
        _metadataService = metadataService;
        _options = options.Value;
    }

    public async Task<MigrationResponseDto> GenerateLoadAsync(
        GenerateLoadCommand command)
    {
        string projectPath =
            Path.Combine(
                _options.Folders.Root,
                command.ProjectName);

        if (!Directory.Exists(projectPath))
        {
            throw new IOException(
                $"El directorio del proyecto '{projectPath}' no existe.");
        }

        string outputFolder =
            Path.Combine(
                projectPath,
                _options.Folders.MigrationTask.DirectoryName, 
                _options.Folders.MigrationTask.DataIngestion);

        if (!Directory.Exists(outputFolder))
        {
            throw new IOException(
                $"El directorio del Tareas de migracion '{outputFolder}' no existe.");
        }

        string artifactPrefix =
            MigrationWarningExtensions.GetArtifactPrefix(
                command.ArtifactType);

        List<string> generatedFiles = [];
        List<string> warnings = [];

        int generatedCount = 0;
        int skippedCount = 0;

        List<TableMetadata> metadata =
            await _metadataService.ExtractMetadataAsync(
                "Target",
                command.Schema,
                command.Tables);

        metadata =
            MetadataNormalizer.NormalizeColumns(metadata);

        // Tablas encontradas
        HashSet<string> existingTables =
            metadata
                .Select(t => t.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        // Reportar las solicitadas que no existen
        foreach (string table in command.Tables)
        {
            if (!existingTables.Contains(table))
            {
                warnings.Add(
                    $"La tabla solicitada '{command.Schema}.{table}' no existe en la base de datos.");
            }
        }

        foreach (TableMetadata targetTable in metadata)
        {
            string artifactFolder =
                Path.Combine(
                    outputFolder,
                    $"{targetTable.Schema}.{targetTable.Name}");

            // Si no existe, lo crea
            Directory.CreateDirectory(
                artifactFolder);

            string fileName =
                $"LOAD_{targetTable.Schema}.{artifactPrefix}_{targetTable.Name}.sql";

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
                BuildLoadScript(
                    targetTable,
                    command.ArtifactType);

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

    #region BuildLoadScript
    private static string BuildLoadScript(
        TableMetadata targetTable,
        ArtifactType artifactType)
    {
        string artifactPrefix =
            MigrationWarningExtensions.GetArtifactPrefix(
                artifactType);

        bool isIntegration =
            artifactType == ArtifactType.Integration;

        string strDeclaration = "";

        string insertSql =
            LoadBuilder.BuildInsert(
                targetTable,
                false);

        string selectSql =
            $"SELECT {LoadBuilder.BuildColumnList(targetTable)}" +
            $"{Environment.NewLine}" +
            $"FROM [{targetTable.Schema}].[{artifactPrefix}_{targetTable.Name}] INTTBL";

        if (isIntegration)
        {
            selectSql +=
                $"{Environment.NewLine}" +
                $"WHERE INTTBL.[ExecutionId] = @ExecutionId" +
                $"{Environment.NewLine}" +
                $"AND INTTBL.[OperationCD] = 'I'";

            strDeclaration = BuildParameterDeclaration(artifactType);
        }

        bool hasIdentity =
            targetTable.Columns.Any(
                c => c.IsIdentity);

        if (hasIdentity)
        {
            return
$"""
/*
====================================================
TABLE : [{targetTable.Schema}].[{targetTable.Name}]
GENERATED BY Sistran.MigrationBuilder

This script preserves IDENTITY values.
====================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY

    SET IDENTITY_INSERT [{targetTable.Schema}].[{targetTable.Name}] ON;

    {insertSql}
    {selectSql};

    SET IDENTITY_INSERT [{targetTable.Schema}].[{targetTable.Name}] OFF;

END TRY
BEGIN CATCH

    BEGIN TRY
        SET IDENTITY_INSERT [{targetTable.Schema}].[{targetTable.Name}] OFF;
    END TRY
    BEGIN CATCH
        -- Ignore cleanup errors.
    END CATCH;

    THROW;

END CATCH
""";
        }

        return
$"""
/*
====================================================
TABLE : [{targetTable.Schema}].[{targetTable.Name}]
GENERATED BY Sistran.MigrationBuilder
====================================================
*/
{strDeclaration}

{insertSql}
{selectSql};
""";
    }
    #endregion


    private static string BuildParameterDeclaration(
        ArtifactType artifactType)
    {
        if (artifactType != ArtifactType.Integration)
        {
            return string.Empty;
        }

        return """
DECLARE
@ExecutionId CHAR(26)

SELECT @ExecutionId = '{{param:ExecutionId}}'

""";
    }


}
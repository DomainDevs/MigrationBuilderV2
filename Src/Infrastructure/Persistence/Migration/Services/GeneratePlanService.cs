using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using DataToolkit.Library;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Persistence.Connect.Context;
using Persistence.Metadata.Services;
using Persistence.Planning.Services;
using Serilog;
using Serilog.Core;
using Shared.Options;
using System.Text.Json;

namespace Persistence.Migration.Services;

public sealed class GeneratePlanService : IGeneratePlanService
{
    private readonly IUnitOfWork _target;
    private readonly MigrationOptions _options;
    private readonly MetadataService _metadataService;
    private readonly DependencyResolverService _dependencyResolver;
    private readonly MigrationPlanningService _migrationPlanningService;

    private readonly IGenerateDdlService _generateDdlService;
    private readonly IGenerateExtractionService _generateExtractionService;
    private readonly IGenerateLoadService _generateLoadService;

    private static readonly Serilog.ILogger Logger =
        Log.ForContext<GeneratePlanService>();

    public GeneratePlanService(
        SqlServerContext context,
        MetadataService metadataService,
        DependencyResolverService dependencyResolver,
        MigrationPlanningService migrationPlanningService,
        IOptions<MigrationOptions> options,

        IGenerateDdlService generateDdlService,
        IGenerateExtractionService generateExtractionService,
        IGenerateLoadService generateLoadService
        )
    {
        _target = context.Target;
        _options = options.Value;
        _metadataService = metadataService;
        _dependencyResolver = dependencyResolver;
        _migrationPlanningService = migrationPlanningService;

        _generateDdlService = generateDdlService;
        _generateExtractionService = generateExtractionService;
        _generateLoadService = generateLoadService;
    }

    public async Task<MigrationGenerationResultDto>
        GenerateMigrationPlanAsync(GeneratePlanCommand command)
    {
        string projectPath = Path.Combine(_options.Folders.Root, command.ProjectName);

        if (!Directory.Exists(projectPath))
            throw new IOException($"El directorio del proyecto '{projectPath}' no existe.");

        string outputFolder = Path.Combine(projectPath, _options.Folders.MigrationTask);

        //Si no existe, lo crea
        Directory.CreateDirectory(outputFolder);

        try { 
            string migrationPlanFile = Path.Combine(projectPath, "MigrationPlan.json");

            List<string> generatedFiles = [];
            List<string> warnings = [];

            Logger.Information(
            "Iniciando generación del plan. Proyecto={Project}, Esquema={Schema}",
            command.ProjectName,
            command.Schema);

            //Destino
            List<TableMetadata> metadata = await _metadataService.ExtractMetadataAsync(false, command.Schema, command.Tables);

            HashSet<string> existingTables = metadata.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string table in command.Tables)
                if (!existingTables.Contains(table))
                    warnings.Add($"La tabla solicitada '{command.Schema}.{table}' no existe en la base de datos.");

            var completeTables = await _dependencyResolver.ResolveDependenciesAsync(command.Schema, command.Tables);

            var allTables = command.Tables.Union(completeTables, StringComparer.OrdinalIgnoreCase).ToList();

            completeTables = await _dependencyResolver.ResolveDependenciesAsync(command.Schema, allTables);

            allTables = allTables.Union(completeTables, StringComparer.OrdinalIgnoreCase).ToList();

            IReadOnlyList<string> executionPlan =
                await _migrationPlanningService.BuildExecutionPlanStringAsyncStr(
                    _target,
                    command.Schema,
                    allTables);

            if (executionPlan.Count == 0)
                throw new IOException(
                    "No se encontraron tablas válidas para generar el plan de migración.");

            Logger.Information(
            "Antes del plan...",
            command.ProjectName,
            command.Schema);

            MigrationPlan? previousPlan = null;
            if (File.Exists(migrationPlanFile))
            {
                try
                {
                    previousPlan = JsonSerializer.Deserialize<MigrationPlan>(
                        await File.ReadAllTextAsync(migrationPlanFile));
                }
                catch
                {
                    previousPlan = null;
                }
            }

            Dictionary<string, MigrationPackage> previousPackages =
                previousPlan?.Packages.ToDictionary(
                    p => p.Package,
                    StringComparer.OrdinalIgnoreCase)
                ?? new(StringComparer.OrdinalIgnoreCase);

            var plan = new MigrationPlan
            {
                Version = "1.0",
                Revision = (previousPlan?.Revision ?? 0) + 1,
                GeneratedAt = DateTime.UtcNow,
                Packages = executionPlan
                    .Select((table, index) =>
                    {
                        string packageName = $"{command.Schema}.{table}";

                        if (previousPackages.TryGetValue(packageName, out MigrationPackage? oldPackage))
                        {
                            return new MigrationPackage
                            {
                                Stage = index + 1,
                                Package = packageName,
                                Enabled = oldPackage.Enabled,
                                Approved = oldPackage.Approved
                            };
                        }

                        return new MigrationPackage
                        {
                            Stage = index + 1,
                            Package = packageName,
                            Enabled = true,
                            Approved = false
                        };
                    })
                    .ToList()
            };

            await File.WriteAllTextAsync(
                migrationPlanFile,
                JsonSerializer.Serialize(
                    plan,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

            generatedFiles.Add(migrationPlanFile);

            Logger.Information(
            "iNICIA DDL...",
            command.ProjectName,
            command.Schema);

            //LLAMADO SERVIOS ARTEFACTOS DDL
            MigrationResponseDto ddlResponse =
                await _generateDdlService.GenerateDdlScriptsAsync(
                    new GenerateDdlCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));


            Logger.Information(
            "INICIA EXTRACCION...",
            command.ProjectName,
            command.Schema);
            //EXTRACCION
            MigrationResponseDto extractionResponse =
                await _generateExtractionService.GenerateExtractionAsync(
                    new GenerateExtractionCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));

            Logger.Information(
            "INICIA LOAD...",
            command.ProjectName,
            command.Schema);
            //LOAD
            MigrationResponseDto loadResponse =
                await _generateLoadService.GenerateLoadAsync(
                    new GenerateLoadCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));
            Logger.Information(
            "FINALIZA ARTEFACTOS...",
            command.ProjectName,
            command.Schema);


            return new MigrationGenerationResultDto
            {
                Artifacts =
                [
                    new MigrationArtifactResultDto
            {
                Artifact = "Plan",
                GeneratedFiles = 1,
                SkippedFiles = 0,
                Files = generatedFiles,
                Warnings = warnings
            },

            new MigrationArtifactResultDto
            {
                Artifact = "DDL",
                GeneratedFiles = ddlResponse.GeneratedFiles,
                SkippedFiles = ddlResponse.SkippedTables,
                Files = ddlResponse.Files,
                Warnings = ddlResponse.Warnings
            },

            new MigrationArtifactResultDto
            {
                Artifact = "Extraction",
                GeneratedFiles = extractionResponse.GeneratedFiles,
                SkippedFiles = extractionResponse.SkippedTables,
                Files = extractionResponse.Files,
                Warnings = extractionResponse.Warnings
            },

            new MigrationArtifactResultDto
            {
                Artifact = "Load",
                GeneratedFiles = loadResponse.GeneratedFiles,
                SkippedFiles = loadResponse.SkippedTables,
                Files = loadResponse.Files,
                Warnings = loadResponse.Warnings
            }
                ]
            };

        }
        catch(Exception ex)
        {
            Logger.Information(
            "Despues del plan..." + ex.Message.ToString(),
            command.ProjectName,
            command.Schema);

            return null;
        }

    }

}

public sealed class MigrationPlan
{
    public string Version { get; set; } = "1.0";
    public int Revision { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<MigrationPackage> Packages { get; set; } = [];
}

public sealed class MigrationPackage
{
    public int Stage { get; set; }
    public string Package { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Approved { get; set; } = false;
}

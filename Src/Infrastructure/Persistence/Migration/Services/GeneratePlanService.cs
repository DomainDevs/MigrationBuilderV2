using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Planning.Services;
using Serilog;
using Shared.Options;
using System.Text.Json;

namespace Persistence.Migration.Services;

public sealed class GeneratePlanService : IGeneratePlanService
{
    private readonly IDatabaseContext _database;
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
        IDatabaseContext database, //SqlServerContext context,
        MetadataService metadataService,
        DependencyResolverService dependencyResolver,
        MigrationPlanningService migrationPlanningService,
        IOptions<MigrationOptions> options,
        IGenerateDdlService generateDdlService,
        IGenerateExtractionService generateExtractionService,
        IGenerateLoadService generateLoadService
    )
    {
        _database = database; //_target = context.Target;
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

        try
        {
            string migrationPlanFile = Path.Combine(projectPath, "MigrationPlan.json");

            List<string> generatedFiles = [];
            List<string> warnings = [];

            // Determinar el alcance de la migración.
            //
            // Si se especifican tablas, se utilizan como punto de partida.
            // Si no se especifican, se obtiene el metadata completo de
            // Source y Target y se toman únicamente las tablas que existen
            // en ambos lados.
            List<string> requestedTables;

            if (command.Tables is { Count: > 0 })
            {
                requestedTables =
                    command.Tables
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                List<TableMetadata> targetMetadata =
                    await _metadataService.ExtractMetadataAsync(
                        "Target",
                        command.Schema,
                        requestedTables);

                HashSet<string> existingTargetTables =
                    targetMetadata
                        .Select(t => t.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (string table in requestedTables)
                {
                    if (!existingTargetTables.Contains(table))
                    {
                        warnings.Add(
                            $"La tabla solicitada '{command.Schema}.{table}' no existe en la base de datos destino.");
                    }
                }
            }
            else
            {
                List<TableMetadata> sourceMetadata =
                    await _metadataService.ExtractMetadataAsync(
                        "Source",
                        command.Schema);

                List<TableMetadata> targetMetadata =
                    await _metadataService.ExtractMetadataAsync(
                        "Target",
                        command.Schema);

                HashSet<string> sourceTables =
                    sourceMetadata
                        .Select(t => t.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                requestedTables =
                    targetMetadata
                        .Select(t => t.Name)
                        .Where(sourceTables.Contains)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
            }

            if (requestedTables.Count == 0)
                throw new IOException(
                    $"No se encontraron tablas comunes entre origen y destino para el esquema '{command.Schema}'.");

            // Resolver el conjunto completo:
            // tablas solicitadas + todas sus dependencias.
            var completeTables =
                await _dependencyResolver.ResolveDependenciesAsync(
                    command.Schema,
                    requestedTables);

            var allTables =
                requestedTables
                    .Union(
                        completeTables,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            completeTables =
                await _dependencyResolver.ResolveDependenciesAsync(
                    command.Schema,
                    allTables);

            allTables =
                allTables
                    .Union(
                        completeTables,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // El planner es la autoridad para determinar el orden.
            IReadOnlyList<string> executionPlan =
                await _migrationPlanningService.BuildExecutionPlanStringAsyncStr(
                    _database["Target"].CreateNew(),
                    command.Schema,
                    allTables);

            if (executionPlan.Count == 0)
                throw new IOException(
                    "No se encontraron tablas válidas para generar el plan de migración.");

            // Garantizar que ninguna tabla solicitada o dependencia resuelta
            // desaparezca del plan si el planner no la devuelve.
            HashSet<string> plannedTables =
                executionPlan
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<string> missingTables =
                allTables
                    .Where(table => !plannedTables.Contains(table))
                    .ToList();

            if (missingTables.Count > 0)
            {
                executionPlan =
                    executionPlan
                        .Concat(missingTables)
                        .ToList();
            }

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
                Reprocess = false,
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
                                Approved = oldPackage.Approved,
                                SelfContainedEtl = oldPackage.SelfContainedEtl
                            };
                        }

                        return new MigrationPackage
                        {
                            Stage = index + 1,
                            Package = packageName,
                            Enabled = true,
                            Approved = false,
                            SelfContainedEtl = false
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

            // Los servicios de artifacts reciben el plan ya ordenado.
            // Cada servicio debe resolver el metadata por tabla para evitar
            // listas masivas de parámetros SQL.
            MigrationResponseDto ddlResponse =
                await _generateDdlService.GenerateDdlScriptsAsync(
                    new GenerateDdlCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));

            MigrationResponseDto extractionResponse =
                await _generateExtractionService.GenerateExtractionAsync(
                    new GenerateExtractionCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));

            MigrationResponseDto loadResponse =
                await _generateLoadService.GenerateLoadAsync(
                    new GenerateLoadCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));

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
        catch (Exception ex)
        {
            Logger.Information(
                "Error: timeout." + ex.Message.ToString(),
                command.ProjectName,
                command.Schema);

            throw;
        }
    }
}

public sealed class MigrationPlan
{
    public string Version { get; set; } = "1.0";
    public int Revision { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool Reprocess { get; set; } = false;
    public List<MigrationPackage> Packages { get; set; } = [];
}

public sealed class MigrationPackage
{
    public int Stage { get; set; }
    public string Package { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Approved { get; set; } = false;
    public bool SelfContainedEtl { get; set; } = false;
}

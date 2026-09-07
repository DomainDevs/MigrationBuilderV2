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
    private readonly CleanScriptGenerator _cleanScriptGenerator;

    private readonly IGenerateDdlService _generateDdlService;
    private readonly IGenerateExtractionService _generateExtractionService;
    private readonly IGenerateLoadService _generateLoadService;

    private static readonly Serilog.ILogger Logger =
        Log.ForContext<GeneratePlanService>();

    public GeneratePlanService(
        IDatabaseContext database,
        MetadataService metadataService,
        DependencyResolverService dependencyResolver,
        MigrationPlanningService migrationPlanningService,
        IOptions<MigrationOptions> options,
        IGenerateDdlService generateDdlService,
        IGenerateExtractionService generateExtractionService,
        IGenerateLoadService generateLoadService,
        CleanScriptGenerator cleanScriptGenerator)
    {
        _database = database;
        _options = options.Value;
        _metadataService = metadataService;
        _dependencyResolver = dependencyResolver;
        _migrationPlanningService = migrationPlanningService;
        _generateDdlService = generateDdlService;
        _generateExtractionService = generateExtractionService;
        _generateLoadService = generateLoadService;
        _cleanScriptGenerator = cleanScriptGenerator;
    }

    public async Task<MigrationGenerationResultDto>
        GenerateMigrationPlanAsync(GeneratePlanCommand command)
    {
        string projectPath = Path.Combine(_options.Folders.Root, command.ProjectName);

        if (!Directory.Exists(projectPath))
            throw new IOException($"El directorio del proyecto '{projectPath}' no existe.");

        string outputFolder = Path.Combine(
            projectPath, _options.Folders.MigrationTask.DirectoryName,
            _options.Folders.MigrationTask.DataIngestion);

        Directory.CreateDirectory(outputFolder);

        string migrationPlanFile = Path.Combine(projectPath, "MigrationPlan.json");

        try
        {
            List<string> generatedFiles = [];
            List<string> warnings = [];

            // 1. El MigrationPlan existente es la fuente de verdad.
            //    Nunca se reconstruye eliminando paquetes anteriores.
            MigrationPlan? existingPlan = await LoadExistingPlanAsync(migrationPlanFile);

            List<string> packageTables = existingPlan?.Packages
                .Select(p => ExtractTableName(p.Package, command.Schema))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            // 2. Determinar las tablas solicitadas.
            //    Si existe un plan y no se especifican tablas, se conserva su alcance.
            //    Si no existe plan, se conserva el comportamiento anterior: tablas comunes.
            List<string> requestedTables;

            if (command.Tables is { Count: > 0 })
            {
                requestedTables = command.Tables
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else if (existingPlan is not null && packageTables.Count > 0)
            {
                requestedTables = packageTables.ToList();
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

                HashSet<string> sourceTables = sourceMetadata
                    .Select(t => t.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                requestedTables = targetMetadata
                    .Select(t => t.Name)
                    .Where(sourceTables.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (requestedTables.Count == 0 && packageTables.Count == 0)
                throw new IOException(
                    $"No se encontraron tablas para generar el plan de migración del esquema '{command.Schema}'.");

            // 3. Detectar solamente las tablas nuevas solicitadas.
            var knownPackages = existingPlan?.Packages
                .GroupBy(
                    p => p.Package,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, MigrationPackage>(StringComparer.OrdinalIgnoreCase);

            List<string> newRequestedTables = [];

            foreach (string table in requestedTables)
            {
                string packageName = BuildPackageName(command.Schema, table);

                if (!knownPackages.ContainsKey(packageName))
                    newRequestedTables.Add(table);
            }

            // 4. Validar existencia en Target solo para las tablas nuevas solicitadas.
            if (newRequestedTables.Count > 0)
            {
                List<TableMetadata> targetMetadata =
                    await _metadataService.ExtractMetadataAsync(
                        "Target",
                        command.Schema,
                        newRequestedTables);

                HashSet<string> existingTargetTables = targetMetadata
                    .Select(t => t.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (string table in newRequestedTables)
                {
                    if (!existingTargetTables.Contains(table))
                    {
                        warnings.Add(
                            $"La tabla solicitada '{command.Schema}.{table}' no existe en la base de datos destino y no se agregará al plan.");
                    }
                }
            }

            // Solo tablas realmente existentes en Target pueden entrar al análisis.
            List<string> validatedRequestedTables;

            if (newRequestedTables.Count > 0)
            {
                List<TableMetadata> targetMetadata =
                    await _metadataService.ExtractMetadataAsync(
                        "Target",
                        command.Schema,
                        newRequestedTables);

                HashSet<string> existingTargetTables = targetMetadata
                    .Select(t => t.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                validatedRequestedTables = requestedTables
                    .Where(table =>
                        packageTables.Contains(table, StringComparer.OrdinalIgnoreCase) ||
                        existingTargetTables.Contains(table))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                validatedRequestedTables = requestedTables;
            }

            if (validatedRequestedTables.Count == 0 && packageTables.Count == 0)
                throw new IOException(
                    $"No se encontraron tablas válidas para generar el plan de migración del esquema '{command.Schema}'.");

            // 5. El conjunto base del plan es SIEMPRE el plan existente + nuevas tablas válidas.
            var planTables = new HashSet<string>(
                packageTables,
                StringComparer.OrdinalIgnoreCase);

            foreach (string table in validatedRequestedTables)
                planTables.Add(table);

            // 6. Resolver dependencias del conjunto completo.
            //    Las dependencias que no estén en el plan se agregan como nuevos paquetes.
            List<string> resolvedTables =
                await _dependencyResolver.ResolveDependenciesAsync(
                    command.Schema,
                    planTables.ToList());

            foreach (string table in resolvedTables)
                planTables.Add(table);

            // 7. El planner calcula el orden del conjunto acumulado.
            IReadOnlyList<string> executionPlan =
                await _migrationPlanningService.BuildExecutionPlanStringAsyncStr(
                    _database["Target"].CreateNew(),
                    command.Schema,
                    planTables.ToList());

            if (executionPlan.Count == 0)
                throw new IOException(
                    "No se encontraron tablas válidas para generar el plan de migración.");

            HashSet<string> plannedTables = executionPlan
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 8. Si el planner no devuelve alguna tabla que sí estaba en el plan,
            //    NO se elimina. Se conserva y se informa.
            List<string> retainedUnplannedTables = planTables
                .Where(table => !plannedTables.Contains(table))
                .OrderBy(table => table, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string table in retainedUnplannedTables)
            {
                warnings.Add(
                    $"La tabla '{command.Schema}.{table}' permanece en el MigrationPlan, pero no fue devuelta por el planner y conserva su posición relativa.");
            }

            // 9. Crear/actualizar paquetes conservando todas sus propiedades.
            //    Los paquetes nuevos se agregan con los valores por defecto.
            var packagesByName = new Dictionary<string, MigrationPackage>(
                StringComparer.OrdinalIgnoreCase);

            if (existingPlan is not null)
            {
                foreach (MigrationPackage package in existingPlan.Packages)
                {
                    if (string.IsNullOrWhiteSpace(package.Package))
                        continue;

                    if (!packagesByName.ContainsKey(package.Package))
                    {
                        packagesByName[package.Package] = new MigrationPackage
                        {
                            Stage = package.Stage,
                            Package = package.Package,
                            Enabled = package.Enabled,
                            Approved = package.Approved,
                            SelfContainedEtl = package.SelfContainedEtl
                        };
                    }
                }
            }

            foreach (string table in planTables)
            {
                string packageName = BuildPackageName(command.Schema, table);

                if (!packagesByName.ContainsKey(packageName))
                {
                    packagesByName[packageName] = new MigrationPackage
                    {
                        Stage = 0,
                        Package = packageName,
                        Enabled = true,
                        Approved = false,
                        SelfContainedEtl = false
                    };
                }
            }

            // 10. Solo se recalcula Stage.
            //     Las propiedades Enabled/Approved/SelfContainedEtl permanecen intactas.
            int nextStage = 1;

            foreach (string table in executionPlan)
            {
                string packageName = BuildPackageName(command.Schema, table);

                if (packagesByName.TryGetValue(packageName, out MigrationPackage? package))
                {
                    package.Stage = nextStage++;
                }
            }

            // 11. Los paquetes que el planner no devolvió permanecen en el plan.
            //     Se colocan después del resultado calculado para evitar colisiones de Stage.
            foreach (string table in retainedUnplannedTables)
            {
                string packageName = BuildPackageName(command.Schema, table);

                if (packagesByName.TryGetValue(packageName, out MigrationPackage? package))
                    package.Stage = nextStage++;
            }

            List<MigrationPackage> finalPackages = packagesByName.Values
                .OrderBy(p => p.Stage)
                .ThenBy(p => p.Package, StringComparer.OrdinalIgnoreCase)
                .ToList();

            MigrationPlan plan = new()
            {
                Version = existingPlan?.Version ?? "1.0",
                Revision = (existingPlan?.Revision ?? 0) + 1,
                GeneratedAt = DateTime.UtcNow,
                Reprocess = existingPlan?.Reprocess ?? false,
                Packages = finalPackages
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

            // 12. Los artifacts se generan con el orden calculado por el planner.
            MigrationResponseDto ddlResponse =
                await _generateDdlService.GenerateDdlScriptsAsync(
                    new GenerateDdlCommand(
                        command.ProjectName,
                        command.Schema,
                        command.ArtifactType,
                        executionPlan.ToList()));

            // 13. Generar estrategia de borrado
            string preHookFolder =
                Path.Combine(_options.Folders.MigrationTask.DirectoryName, _options.Folders.MigrationTask.PreHook);

            Directory.CreateDirectory(preHookFolder);

            string cleanFile =
                Path.Combine(
                    preHookFolder,
                    "01_CLEAN_MigrationPlan.sql");

            if (!File.Exists(cleanFile))
            {
                string cleanScript =
                    _cleanScriptGenerator.Generate(plan);

                await File.WriteAllTextAsync(
                    cleanFile,
                    cleanScript);

                generatedFiles.Add(cleanFile);
            }
            else
            {
                warnings.Add(
                    $"El archivo '{cleanFile}' ya existe, no se actualiza.");
            }

            // 14. Generar reponse
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
            Logger.Error(
                ex,
                "Error generando el plan de migración para {ProjectName}/{Schema}.",
                command.ProjectName,
                command.Schema);

            throw;
        }
    }

    private static async Task<MigrationPlan?> LoadExistingPlanAsync(string migrationPlanFile)
    {
        if (!File.Exists(migrationPlanFile))
            return null;

        try
        {
            string json = await File.ReadAllTextAsync(migrationPlanFile);

            return JsonSerializer.Deserialize<MigrationPlan>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException ex)
        {
            throw new IOException(
                $"El MigrationPlan '{migrationPlanFile}' no es un JSON válido. El plan existente no será sobrescrito.",
                ex);
        }
    }

    private static string BuildPackageName(string schema, string table)
        => $"{schema}.{table}";

    private static string ExtractTableName(string packageName, string schema)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return string.Empty;

        string prefix = $"{schema}.";

        if (packageName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return packageName[prefix.Length..];

        int separator = packageName.IndexOf('.');
        return separator >= 0
            ? packageName[(separator + 1)..]
            : packageName;
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

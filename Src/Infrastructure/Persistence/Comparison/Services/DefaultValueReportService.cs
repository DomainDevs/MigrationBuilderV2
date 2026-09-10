using DataToolkit.Library;
using Microsoft.Extensions.Options;
using Persistence.Metadata.Services;
using Persistence.Migration.Helpers;
using Persistence.Migration.Metadata;
using Persistence.Migration.Services;
using Shared.Options;
using System.Text.Json;

namespace Persistence.Comparison.Services;

/// <summary>
/// Genera el reporte de columnas que existen en Target pero no existen
/// en Source y que, por lo tanto, recibirán un valor por defecto durante
/// la generación del script de extracción.
///
/// El alcance del reporte está determinado exclusivamente por los paquetes
/// habilitados del MigrationPlan.json y cuya carpeta de paquete existe.
/// </summary>
public sealed class DefaultValueReportService
{
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _options;

    public DefaultValueReportService(
        MetadataService metadataService,
        IOptions<MigrationOptions> options)
    {
        _metadataService = metadataService;
        _options = options.Value;
    }

    /// <summary>
    /// Genera el reporte de valores por defecto para las tablas
    /// habilitadas del proyecto.
    /// </summary>
    public async Task<DefaultValueReport> GenerateAsync(
        string projectName,
        string schema)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException(
                "El nombre del proyecto es obligatorio.",
                nameof(projectName));

        if (string.IsNullOrWhiteSpace(schema))
            throw new ArgumentException(
                "El esquema es obligatorio.",
                nameof(schema));

        string projectPath =
            Path.Combine(
                _options.Folders.Root,
                projectName);

        if (!Directory.Exists(projectPath))
            throw new IOException(
                $"El directorio del proyecto '{projectPath}' no existe.");

        string migrationPlanFile =
            Path.Combine(
                projectPath,
                "MigrationPlan.json");

        if (!File.Exists(migrationPlanFile))
            throw new IOException(
                $"No existe el MigrationPlan '{migrationPlanFile}'.");

        MigrationPlan plan =
            await LoadMigrationPlanAsync(
                migrationPlanFile);

        List<string> tables =
            GetEnabledPackageTables(
                plan,
                projectPath,
                schema);

        if (tables.Count == 0)
        {
            return new DefaultValueReport
            {
                ProjectName = projectName,
                Schema = schema,
                TablesAnalyzed = 0,
                ColumnsRequiringDefault = 0
            };
        }

        Task<List<TableMetadata>> sourceTask =
            _metadataService.ExtractMetadataAsync(
                "Source",
                schema,
                tables);

        Task<List<TableMetadata>> targetTask =
            _metadataService.ExtractMetadataAsync(
                "Target",
                schema,
                tables);

        await Task.WhenAll(
            sourceTask,
            targetTask);

        List<TableMetadata> source =
            MetadataNormalizer.NormalizeColumns(
                sourceTask.Result);

        List<TableMetadata> target =
            MetadataNormalizer.NormalizeColumns(
                targetTask.Result);

        DefaultValueReport report = new()
        {
            ProjectName = projectName,
            Schema = schema,
            TablesAnalyzed = tables.Count
        };

        Dictionary<string, TableMetadata> sourceLookup =
            source.ToDictionary(
                GetTableKey,
                StringComparer.OrdinalIgnoreCase);

        Dictionary<string, TableMetadata> targetLookup =
            target.ToDictionary(
                GetTableKey,
                StringComparer.OrdinalIgnoreCase);

        foreach (string tableName in tables.Order(
                     StringComparer.OrdinalIgnoreCase))
        {
            string tableKey =
                $"{schema}.{tableName}";

            if (!targetLookup.TryGetValue(
                    tableKey,
                    out TableMetadata? targetTable))
            {
                continue;
            }

            sourceLookup.TryGetValue(
                tableKey,
                out TableMetadata? sourceTable);

            if (sourceTable is null)
                continue;

            Dictionary<string, ColumnMetadata> sourceColumns =
                sourceTable.Columns.ToDictionary(
                    c => c.Name,
                    StringComparer.OrdinalIgnoreCase);

            foreach (ColumnMetadata targetColumn in
                     targetTable.Columns)
            {
                if (sourceColumns.ContainsKey(targetColumn.Name))
                    continue;

                string defaultValue =
                    MigrationWarningExtensions.GetDefaultValue(
                        targetColumn);

                report.Items.Add(
                    new DefaultValueReportItem
                    {
                        Table = GetTableKey(targetTable),
                        Column = targetColumn.Name,
                        SqlType = targetColumn.SqlType,
                        DefaultValue = defaultValue
                    });
            }
        }

        report.ColumnsRequiringDefault =
            report.Items.Count;

        return report;
    }

    private List<string> GetEnabledPackageTables(
        MigrationPlan plan,
        string projectPath,
        string schema)
    {
        List<string> packages =
            plan.Packages
                .Where(static p => p.Enabled)
                .Select(static p => p.Package)
                .Where(static p =>
                    !string.IsNullOrWhiteSpace(p))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        string dataIngestionPath =
            Path.Combine(
                projectPath,
                _options.Folders.MigrationTask.DirectoryName,
                _options.Folders.MigrationTask.DataIngestion);

        packages.RemoveAll(package =>
        {
            string packagePath =
                Path.Combine(
                    dataIngestionPath,
                    package);

            return !Directory.Exists(packagePath);
        });

        return packages
            .Select(package =>
                ExtractTableName(
                    package,
                    schema))
            .Where(static table =>
                !string.IsNullOrWhiteSpace(table))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ExtractTableName(
        string packageName,
        string schema)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return string.Empty;

        string prefix =
            $"{schema}.";

        if (packageName.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return packageName[prefix.Length..];
        }

        int separator =
            packageName.IndexOf('.');

        return separator >= 0
            ? packageName[(separator + 1)..]
            : packageName;
    }

    private static string GetTableKey(
        TableMetadata table)
    {
        return $"{table.Schema}.{table.Name}";
    }

    private static async Task<MigrationPlan>
        LoadMigrationPlanAsync(
            string migrationPlanFile)
    {
        string json =
            await File.ReadAllTextAsync(
                migrationPlanFile);

        MigrationPlan? plan =
            JsonSerializer.Deserialize<MigrationPlan>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (plan is null)
            throw new IOException(
                $"No fue posible cargar el MigrationPlan '{migrationPlanFile}'.");

        return plan;
    }

    /// <summary>
    /// Resultado completo del reporte.
    /// </summary>
    public sealed class DefaultValueReport
    {
        public string ProjectName { get; init; } = string.Empty;

        public string Schema { get; init; } = string.Empty;

        public int TablesAnalyzed { get; init; }

        public int ColumnsRequiringDefault { get; set; }

        public List<DefaultValueReportItem> Items { get; init; } = [];
    }

    /// <summary>
    /// Representa una columna de Target que no existe en Source
    /// y que recibirá un valor por defecto durante la extracción.
    /// </summary>
    public sealed class DefaultValueReportItem
    {
        public string Table { get; init; } = string.Empty;

        public string Column { get; init; } = string.Empty;

        public string SqlType { get; init; } = string.Empty;

        public string DefaultValue { get; init; } = string.Empty;
    }
}

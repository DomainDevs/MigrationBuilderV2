using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Persistence.Migration.Services;
using Shared.Options;
using System.Text.Json;

namespace Persistence.Comparison.Services;

internal sealed class RecordCountService : IRecordCountService
{
    private readonly IDatabaseContext _database;
    private readonly MigrationOptions _options;

    public RecordCountService(IDatabaseContext database)
    {
        _database = database;
    }

    public async Task<List<RecordCountResultDto>> CompareAsync(
        CompareRecordCountCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Source);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Target);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Schema);

        string schema = command.Schema;

        List<string> requestedTables;

        requestedTables = command.Tables?
            .Where(static table => !string.IsNullOrWhiteSpace(table))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];

        if (requestedTables.Count == 0 &&
            !string.IsNullOrWhiteSpace(command.ProjectName))
        {
            requestedTables =
                ObtenerTablasDelMigrationPlan(command.ProjectName);
        }

        using var source = _database[command.Source].CreateNew();
        using var target = _database[command.Target].CreateNew();

        Task<Dictionary<string, long>> sourceTask =
            GetRecordCountsAsync(source, schema);

        Task<Dictionary<string, long>> targetTask =
            GetRecordCountsAsync(target, schema);

        await Task.WhenAll(sourceTask, targetTask);

        Dictionary<string, long> sourceCounts = sourceTask.Result;
        Dictionary<string, long> targetCounts = targetTask.Result;

        IEnumerable<string> tables = requestedTables.Count > 0
            ? requestedTables
            : sourceCounts.Keys
                .Union(
                    targetCounts.Keys,
                    StringComparer.OrdinalIgnoreCase);

        List<RecordCountResultDto> results = [];

        foreach (string table in tables)
        {
            bool sourceExists =
                sourceCounts.TryGetValue(table, out long sourceCount);

            bool targetExists =
                targetCounts.TryGetValue(table, out long targetCount);

            string status = !sourceExists
                ? "SOURCE_NOT_FOUND"
                : !targetExists
                    ? "TARGET_NOT_FOUND"
                    : sourceCount == targetCount
                        ? "OK"
                        : "DIFFERENT";

            results.Add(new RecordCountResultDto
            {
                Schema = schema,
                Table = table,
                SourceCount = sourceExists ? sourceCount : null,
                TargetCount = targetExists ? targetCount : null,
                Status = status
            });
        }

        return results;
    }

    private static async Task<Dictionary<string, long>> GetRecordCountsAsync(
        IUnitOfWork database,
        string schema)
    {
        const string sql = """
            SELECT
                t.name AS TableName,
                ISNULL(SUM(p.rows), 0) AS TotalRows
            FROM sys.tables AS t
            INNER JOIN sys.schemas AS s
                ON s.schema_id = t.schema_id
            INNER JOIN sys.partitions AS p
                ON p.object_id = t.object_id
            WHERE s.name = @Schema
              AND p.index_id IN (0, 1)
            GROUP BY t.name;
            """;

        var parameters = new Dictionary<string, object>
        {
            ["Schema"] = schema
        };

        var queryResults =
            await database.Sql.FromSqlAsync<TableCountRawResult>(
                sql,
                parameters,
                commandTimeout: 1500);

        return queryResults.ToDictionary(
            static x => x.TableName,
            static x => x.TotalRows,
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed class TableCountRawResult
    {
        public string TableName { get; set; } = string.Empty;
        public long TotalRows { get; set; }
    }

    private List<string> ObtenerTablasDelMigrationPlan(
        string projectName)
    {
        string projectPath =
            Path.Combine(
                _options.Folders.Root,
                projectName);

        string planPath =
            Path.Combine(
                projectPath,
                "MigrationPlan.json");

        if (!File.Exists(planPath))
        {
            throw new FileNotFoundException(
                $"No se encontró el MigrationPlan.json del proyecto '{projectName}'.",
                planPath);
        }

        string json = File.ReadAllText(planPath);

        MigrationPlan? plan =
            JsonSerializer.Deserialize<MigrationPlan>(json);

        if (plan is null)
        {
            throw new InvalidOperationException(
                $"No se pudo leer el MigrationPlan del proyecto '{projectName}'.");
        }

        return plan.Packages
            .Where(static x => x.Enabled)
            .Select(static x => x.Package)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
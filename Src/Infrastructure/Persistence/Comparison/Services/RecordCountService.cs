using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;

namespace Persistence.Comparison.Services;

internal sealed class RecordCountService : IRecordCountService
{
    private readonly IDatabaseContext _database;

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
        ArgumentNullException.ThrowIfNull(command.Tables);

        string schema = command.Schema;

        List<string> tables = command.Tables
            .Where(static table => !string.IsNullOrWhiteSpace(table))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tables.Count == 0)
            return [];

        using var source = _database[command.Source].CreateNew();
        using var target = _database[command.Target].CreateNew();

        // Una consulta por base de datos.
        // Ambas bases se consultan en paralelo, sin generar carga
        // adicional dentro de cada base.
        Task<Dictionary<string, long>> sourceTask =
            GetRecordCountsAsync(source, schema);

        Task<Dictionary<string, long>> targetTask =
            GetRecordCountsAsync(target, schema);

        await Task.WhenAll(sourceTask, targetTask);

        Dictionary<string, long> sourceCounts = sourceTask.Result;
        Dictionary<string, long> targetCounts = targetTask.Result;

        List<RecordCountResultDto> results = new(tables.Count);

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
            FROM sys.tables t
            INNER JOIN sys.schemas s
                ON s.schema_id = t.schema_id
            INNER JOIN sys.partitions p
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
                parameters);

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
}
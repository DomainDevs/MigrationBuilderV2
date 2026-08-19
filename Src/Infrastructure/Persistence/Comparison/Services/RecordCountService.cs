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

        using var source =
            _database[command.Source].CreateNew();

        using var target =
            _database[command.Target].CreateNew();

        List<RecordCountResultDto> results =
            new(command.Tables.Count);

        foreach (string table in command.Tables)
        {
            if (string.IsNullOrWhiteSpace(table))
                continue;

            bool sourceExists =
                await TableExistsAsync(
                    source,
                    command.Schema!,
                    table);

            bool targetExists =
                await TableExistsAsync(
                    target,
                    command.Schema!,
                    table);

            long? sourceCount = null;
            long? targetCount = null;

            if (sourceExists)
            {
                sourceCount =
                    await GetCountAsync(
                        source,
                        command.Schema!,
                        table);
            }

            if (targetExists)
            {
                targetCount =
                    await GetCountAsync(
                        target,
                        command.Schema!,
                        table);
            }

            string status =
                !sourceExists
                    ? "SOURCE_NOT_FOUND"
                    : !targetExists
                        ? "TARGET_NOT_FOUND"
                        : sourceCount == targetCount
                            ? "OK"
                            : "DIFFERENT";

            results.Add(new RecordCountResultDto
            {
                Schema = command.Schema!,
                Table = table,
                SourceCount = sourceCount,
                TargetCount = targetCount,
                Status = status
            });
        }

        return results;
    }

    private static async Task<bool> TableExistsAsync(
        IUnitOfWork database,
        string schema,
        string table)
    {
        const string sql = """
            SELECT COUNT_BIG(*)
            FROM sys.tables AS t
            INNER JOIN sys.schemas AS s
                ON s.schema_id = t.schema_id
            WHERE s.name = @Schema
              AND t.name = @Table
            """;

        long count =
            (await database.Sql.FromSqlAsync<long>(
                sql,
                new
                {
                    Schema = schema,
                    Table = table
                }))
            .Single();

        return count > 0;
    }

    private static async Task<long> GetCountAsync(
        IUnitOfWork database,
        string schema,
        string table)
    {
        string quotedSchema = QuoteIdentifier(schema);
        string quotedTable = QuoteIdentifier(table);

        string sql = $"""
            SELECT COUNT_BIG(*)
            FROM {quotedSchema}.{quotedTable}
            """;

        return
            (await database.Sql.FromSqlAsync<long>(sql))
            .Single();
    }

    private static string QuoteIdentifier(string value)
    {
        return $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
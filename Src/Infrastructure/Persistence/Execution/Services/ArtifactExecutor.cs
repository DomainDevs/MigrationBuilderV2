using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Persistence.Configuration;
using Persistence.Execution.ETL;
using Persistence.Execution.Validation;
using Persistence.Metadata.Services;
using Shared.Options;
using System.Text;

namespace Persistence.Execution.Services;

public sealed class ArtifactExecutor
{
    private const long LargeTableThreshold = 1_000_000;

    private readonly IDatabaseContext _database;
    private readonly IConfiguration _configuration;
    private readonly IBulkTransferEngine _bulk;
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _migrationOptions;
    private readonly ConnectionConfig _sourceConfig;
    private readonly ConnectionConfig _targetConfig;

    public ArtifactExecutor(
        IDatabaseContext database,
        MetadataService metadataService,
        IConfiguration configuration,
        IBulkTransferEngine bulk)
    {
        _database = database;
        _metadataService = metadataService;
        _configuration = configuration;
        _bulk = bulk;

        _sourceConfig =
            configuration
                .GetSection("Connections:Source")
                .Get<ConnectionConfig>()
            ?? throw new InvalidOperationException(
                "No se encontró la configuración Connections:Source.");

        _targetConfig =
            configuration
                .GetSection("Connections:Target")
                .Get<ConnectionConfig>()
            ?? throw new InvalidOperationException(
                "No se encontró la configuración Connections:Target.");

        _migrationOptions =
            configuration
                .GetSection(MigrationOptions.SectionName)
                .Get<MigrationOptions>()
            ?? new MigrationOptions();
    }

    public async Task ExecuteAsync(string artifactPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);

        if (!File.Exists(artifactPath))
        {
            throw new FileNotFoundException(
                "No se encontró el artefacto.",
                artifactPath);
        }

        string extension =
            Path.GetExtension(artifactPath);

        if (extension.Equals(
                ".dtsx",
                StringComparison.OrdinalIgnoreCase))
        {
            DTExecRunner.EjecutarPaqueteETL(
                artifactPath,
                _sourceConfig,
                _targetConfig);

            return;
        }

        if (!extension.Equals(
                ".sql",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"El artefacto '{extension}' no es compatible.");
        }

        string sql =
            await File.ReadAllTextAsync(artifactPath);

        sql = RemoveComments(sql);

        if (string.IsNullOrWhiteSpace(sql))
            return;

        SqlScriptValidator.Validate(sql);

        ArtifactInfo artifact =
            ParseArtifact(
                artifactPath);

        if (artifact.Type is ArtifactType.Begin or ArtifactType.End)
        {
            using IUnitOfWork target =
                _database["Target"].CreateNew();

            await ExecuteSqlBatchesAsync(
                target,
                sql);

            return;
        }

        List<string> tables =
            [artifact.RawTable];

        if (artifact.Type is ArtifactType.Local)
        {
            using IUnitOfWork target =
                _database["Target"].CreateNew();

            await ValidateForeignKeysAsync(
                target,
                artifact.Schema,
                artifact.Table);

            await ExecuteSqlBatchesAsync(
                target,
                sql);

            return;
        }

        using IUnitOfWork source =
            _database["Source"].CreateNew();

        using IUnitOfWork targetDatabase =
            _database["Target"].CreateNew();

        long sourceCount =
            await GetSourceRecordCountAsync(
                source,
                artifact.Schema,
                artifact.Table);

        if (sourceCount != 0)
        {
            if (sourceCount > LargeTableThreshold)
            {
                tables = [artifact.Table];
            }

            await ValidateForeignKeysAsync(
                targetDatabase,
                artifact.Schema,
                artifact.Table);
        }

        if (artifact.Type is ArtifactType.Sql)
        {
            await ExecuteBulkTransferAsync(
                sql,
                artifact.Schema,
                tables);

            return;
        }

        await ExecuteSqlBatchesAsync(
            targetDatabase,
            sql);
    }

    private static ArtifactInfo ParseArtifact(
        string artifactPath)
    {
        string fileName =
            Path.GetFileNameWithoutExtension(
                artifactPath);

        string[] parts =
            fileName.Split(
                '_',
                2);

        if (parts.Length != 2)
        {
            throw new InvalidOperationException(
                $"El nombre del artefacto '{fileName}' no tiene el formato esperado.");
        }

        string artifactType =
            parts[0];

        string[] tableParts =
            parts[1].Split(
                '.',
                2);

        if (tableParts.Length != 2)
        {
            throw new InvalidOperationException(
                $"El nombre del artefacto '{fileName}' no contiene un esquema y una tabla válidos.");
        }

        string schema =
            tableParts[0];

        string rawTable =
            tableParts[1];

        string table =
            rawTable
                .Replace(
                    "WF_",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Replace(
                    "STG_",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase);

        ArtifactType type =
            artifactType.ToUpperInvariant() switch
            {
                "BEGIN" => ArtifactType.Begin,
                "END" => ArtifactType.End,
                "LOCAL" => ArtifactType.Local,
                "SQL" => ArtifactType.Sql,
                _ => ArtifactType.Other
            };

        return new ArtifactInfo(
            type,
            schema,
            table,
            rawTable);
    }

    private static async Task<long> GetSourceRecordCountAsync(
        IUnitOfWork source,
        string schema,
        string table)
    {
        const string sql = """
            SELECT
                ISNULL(SUM(p.rows), 0)
            FROM sys.tables t
            INNER JOIN sys.schemas s
                ON s.schema_id = t.schema_id
            INNER JOIN sys.partitions p
                ON p.object_id = t.object_id
            WHERE s.name = @Schema
              AND t.name = @Table
              AND p.index_id IN (0, 1);
            """;

        var parameters = new
        {
            Schema = schema,
            Table = table
        };

        IEnumerable<long> result =
            await source.Sql.FromSqlAsync<long>(
                sql,
                parameters);

        return result.FirstOrDefault();
    }

    private async Task ExecuteSqlBatchesAsync(
        IUnitOfWork target,
        string sql)
    {
        foreach (string batch in SplitBatches(sql))
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            await target.Sql.ExecuteAsync(
                batch,
                commandTimeout: _targetConfig.TimeOut);
        }
    }

    private async Task ExecuteBulkTransferAsync(
        string sql,
        string schema,
        List<string> tables)
    {
        await using SqlConnection sourceConnection =
            new(_configuration.GetConnectionString("Source"));

        await using SqlConnection targetConnection =
            new(_configuration.GetConnectionString("Target"));

        await sourceConnection.OpenAsync();
        await targetConnection.OpenAsync();

        BulkTransferOptions options = new()
        {
            BatchSize =
                _configuration.GetValue<int>(
                    "Migration:Execution:BatchSize"),

            Timeout =
                _configuration.GetValue<int>(
                    "Migration:Execution:BulkCopyTimeout")
        };

        List<TableMetadata> artifactMetadata =
            await _metadataService.ExtractMetadataAsync(
                "Target",
                schema,
                tables);

        if (artifactMetadata.Count == 0)
        {
            throw new InvalidOperationException(
                $"No se encontró metadata para '{schema}.{tables[0]}'.");
        }

        if (artifactMetadata.Count > 1)
        {
            throw new InvalidOperationException(
                $"Se encontró más de una definición para la tabla '{schema}.{tables[0]}'.");
        }

        await _bulk.TransferAsync(
            sourceConnection,
            targetConnection,
            sql,
            artifactMetadata[0],
            options);
    }

    private static string RemoveComments(
        string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        IList<ParseError> errors;

        IList<TSqlParserToken> tokens =
            new TSql170Parser(false)
                .GetTokenStream(
                    new StringReader(sql),
                    out errors);

        StringBuilder builder =
            new(sql.Length);

        foreach (TSqlParserToken token in tokens)
        {
            if (token.TokenType is
                TSqlTokenType.MultilineComment or
                TSqlTokenType.SingleLineComment)
            {
                continue;
            }

            builder.Append(token.Text);
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitBatches(
        string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        StringBuilder batch =
            new();

        using StringReader reader =
            new(sql);

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim().Equals(
                    "GO",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (batch.Length > 0)
                {
                    yield return batch.ToString();
                    batch.Clear();
                }

                continue;
            }

            batch.AppendLine(line);
        }

        if (batch.Length > 0)
            yield return batch.ToString();
    }

    private async Task ValidateForeignKeysAsync(
        IUnitOfWork target,
        string schema,
        string table)
    {
        List<TableMetadata> metadata =
            await _metadataService.ExtractMetadataAsync(
                "Target",
                schema,
                [table]);

        if (metadata.Count == 0)
            return;

        if (metadata.Count > 1)
        {
            throw new InvalidOperationException(
                $"Se encontró más de una definición para la tabla '{schema}.{table}'.");
        }

        TableMetadata tableMetadata =
            metadata[0];

        IEnumerable<string> foreignTables =
            tableMetadata.Columns
                .Where(static x =>
                    !string.IsNullOrWhiteSpace(
                        x.ForeignTable))
                .Select(static x =>
                    x.ForeignTable!)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase);

        foreach (string foreignTable in foreignTables)
        {
            if (foreignTable.Equals(
                    table,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string sql = $"""
                SELECT TOP (1) 1
                FROM [{schema}].[{foreignTable}];
                """;

            IEnumerable<int> result =
                await target.Sql.FromSqlAsync<int>(
                    sql);

            if (!result.Any())
            {
                throw new InvalidOperationException(
                    $"No es posible migrar la tabla '{schema}.{table}' porque la tabla padre '{schema}.{foreignTable}' no contiene registros.");
            }
        }
    }

    private readonly record struct ArtifactInfo(
        ArtifactType Type,
        string Schema,
        string Table,
        string RawTable);

    private enum ArtifactType
    {
        Other,
        Begin,
        End,
        Local,
        Sql
    }
}
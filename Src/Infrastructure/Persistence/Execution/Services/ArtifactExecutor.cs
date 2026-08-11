using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Persistence.Configuration;
using Persistence.Connect.Context;
using Persistence.Execution.ETL;
using Persistence.Execution.Validation;
using Persistence.Metadata.Services;
using Shared.Options;
using System.Text;

namespace Persistence.Execution.Services;

public sealed class ArtifactExecutor
{
    private readonly IDatabaseContext _database;
    //private readonly IUnitOfWork _source;
    //private readonly IUnitOfWork _target;
    private readonly IConfiguration _configuration;
    private readonly IBulkTransferEngine _bulk;
    private readonly MetadataService _metadataService;
    private readonly MigrationOptions _migrationOptions;

    private readonly ConnectionConfig _sourceConfig;
    private readonly ConnectionConfig _targetConfig;

    public ArtifactExecutor(
        //SqlServerContext context,
        IDatabaseContext database,
        MetadataService metadataService,
        IConfiguration configuration,
        IBulkTransferEngine bulk)
    {
        _database = database;
        //_source = context.Source;
        //_target = context.Target;
        _configuration = configuration;
        _bulk = bulk;
        _metadataService = metadataService;

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

    }

    public async Task ExecuteAsync(string artifactPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);

        if (!File.Exists(artifactPath))
            throw new FileNotFoundException(
                "No se encontró el artefacto.",
                artifactPath);


        using var source = _database["Source"].CreateNew();
        using var target = _database["Target"].CreateNew();
        //using var source = _source.CreateNew();
        //using var target = _target.CreateNew();

        string extension = Path.GetExtension(artifactPath);

        switch (extension.ToLowerInvariant())
        {
            case ".sql":
                break;

            case ".dtsx":

                ConnectionConfig sourceConfig =
                    _configuration.GetSection("SourceDB").Get<ConnectionConfig>()!;

                ConnectionConfig targetConfig =
                    _configuration.GetSection("DestinationDB").Get<ConnectionConfig>()!;

                DTExecRunner.EjecutarPaqueteETL(
                    artifactPath,
                    sourceConfig,
                    targetConfig);

                return;

            default:
                throw new NotSupportedException(
                    $"El artefacto '{extension}' no es compatible.");
        }

        string sql = await File.ReadAllTextAsync(artifactPath);

        sql = RemoveComments(sql);

        if (string.IsNullOrWhiteSpace(sql))
            return;

        SqlScriptValidator.Validate(sql);

        string fileName = Path.GetFileNameWithoutExtension(artifactPath);

        string[] parts = fileName.Split('_', 2);

        string artifactType = parts[0];

        string[] tableParts = parts[1].Split('.');

        string schema = tableParts[0];

        string table = tableParts[1]
            .Replace("WF_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("STG_", "", StringComparison.OrdinalIgnoreCase);

        List<string> tables = [table];

        bool isLocal =
            artifactType.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);

        if (isLocal)
        {
            await ValidateForeignKeysAsync(
                target,
                schema,
                table);
        }
        else
        {
            if(!artifactType.Equals("BEGIN", StringComparison.OrdinalIgnoreCase) && !artifactType.Equals("END", StringComparison.OrdinalIgnoreCase))
            {
                string sqlOrigin = $"""
                SELECT COUNT(*)
                FROM [{schema}].[{table}]
                """;

                long totalOrigin =
                    (await source.Sql.FromSqlAsync<long>(sqlOrigin))
                    .Single();

                if (totalOrigin != 0)
                {
                    await ValidateForeignKeysAsync(
                        target,
                        schema,
                        table);
                }
            }


        }

        if (artifactType.Equals("SQL", StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteBulkTransferAsync(
                sql,
                schema,
                tables);

            return;
        }

        await ExecuteSqlBatchesAsync(
            target,
            sql);
    }

    private async Task ExecuteSqlBatchesAsync(
        IUnitOfWork target,
        string sql)
    {
        foreach (string batch in SplitBatches(sql))
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            await target.Sql.ExecuteAsync(batch, 
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
            BatchSize = _configuration.GetValue<int>("Migration:Execution:BatchSize"),
            Timeout = _configuration.GetValue<int>("Migration:Execution:BulkCopyTimeout")
        };

        List<TableMetadata> artifactTable =
            await _metadataService.ExtractMetadataAsync(
                "Target",
                schema,
                tables);

        await _bulk.TransferAsync(
            sourceConnection,
            targetConnection,
            sql,
            artifactTable.Single(),
            options);
    }

    private static string RemoveComments(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        IList<ParseError> errors;

        IList<TSqlParserToken> tokens =
            new TSql170Parser(false)
                .GetTokenStream(new StringReader(sql), out errors);

        StringBuilder builder = new();

        foreach (TSqlParserToken token in tokens)
        {
            if (token.TokenType is TSqlTokenType.MultilineComment
                or TSqlTokenType.SingleLineComment)
            {
                continue;
            }

            builder.Append(token.Text);
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitBatches(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        StringBuilder batch = new();

        using StringReader reader = new(sql);

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
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
        List<TableMetadata> tables =
            await _metadataService.ExtractMetadataAsync(
                "Target",
                schema,
                [table]);

        TableMetadata metadata = tables.Single();

        IEnumerable<ColumnMetadata> foreignKeys =
            metadata.Columns
                .Where(x => !string.IsNullOrWhiteSpace(x.ForeignTable));

        foreach (ColumnMetadata foreignKey in foreignKeys)
        {
            // Ignora claves foráneas autorreferenciadas.
            if (foreignKey.ForeignTable.Equals(
                table,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string sql = $"""
            SELECT COUNT(*)
            FROM [{schema}].[{foreignKey.ForeignTable}]
            """;

            long total =
                (await target.Sql.FromSqlAsync<long>(sql))
                .Single();

            if (total == 0)
            {
                throw new InvalidOperationException(
                    $"No es posible migrar la tabla '{schema}.{table}' porque la tabla padre '{schema}.{foreignKey.ForeignTable}' no contiene registros.");
            }
        }
    }

}

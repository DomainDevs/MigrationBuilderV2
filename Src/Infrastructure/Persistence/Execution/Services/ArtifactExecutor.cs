using DataToolkit.BulkTransfer.Abstractions;
using DataToolkit.BulkTransfer.Core;
using DataToolkit.Library;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Persistence.Connect.Context;
using Persistence.Execution.Validation;
using Persistence.Metadata.Services;
using System.Text;

namespace Persistence.Execution.Services;

public sealed class ArtifactExecutor
{
    private readonly IUnitOfWork _source;
    private readonly IUnitOfWork _target;
    private readonly IConfiguration _configuration;
    private readonly IBulkTransferEngine _bulk;
    private readonly MetadataService _metadataService;

    public ArtifactExecutor(
        SqlServerContext context,
        MetadataService metadataService,
        IConfiguration configuration,
        IBulkTransferEngine bulk)
    {
        _source = context.Source;
        _target = context.Target;
        _configuration = configuration;
        _bulk = bulk;
        _metadataService = metadataService;
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

        string sql = await File.ReadAllTextAsync(artifactPath);

        sql = RemoveComments(sql);

        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        SqlScriptValidator.Validate(sql);

        string fileName =
            Path.GetFileNameWithoutExtension(artifactPath);

        string[] parts = fileName.Split('_', 2);

        string artifactType = parts[0];
        string[] tableParts = parts[1].Split('.');

        string schema = tableParts[0];
        string table = tableParts[1];

        table = table
            .Replace("WF_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("STG_", "", StringComparison.OrdinalIgnoreCase);

        List<string> tables = [table];

        await ValidateForeignKeysAsync(
            schema,
            table);

        if (artifactType.Equals(
            "SQL",
            StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteBulkTransferAsync(
                sql,
                schema,
                tables);

            return;
        }

        //await _target.Sql.ExecuteAsync(sql);
        foreach (string batch in SplitBatches(sql))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await _target.Sql.ExecuteAsync(batch);
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
            BatchSize = 5000,
            Timeout = 1000
        };

        List<TableMetadata> artifactTable =
            await _metadataService.ExtractMetadataAsync(
                false,
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
                .GetTokenStream(
                    new StringReader(sql),
                    out errors);

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
        {
            yield return batch.ToString();
        }
    }

    private async Task ValidateForeignKeysAsync(
        string schema,
        string table)
    {
        List<TableMetadata> tables =
            await _metadataService.ExtractMetadataAsync(
                false,
                schema,
                [table]);

        TableMetadata metadata = tables.Single();

        IEnumerable<ColumnMetadata> foreignKeys =
            metadata.Columns
                .Where(x => !string.IsNullOrWhiteSpace(x.ForeignTable));

        foreach (ColumnMetadata foreignKey in foreignKeys)
        {
            string sql = $"""
            SELECT COUNT(*)
        FROM [{schema}].[{foreignKey.ForeignTable}]
        """;

            long total = (await _target.Sql.FromSqlAsync<long>(sql))
                .Single();


            if (total == 0)
            {
                throw new InvalidOperationException(
                    $"No es posible migrar la tabla '{schema}.{table}' porque la tabla padre '{schema}.{foreignKey.ForeignTable}' no contiene registros.");
            }
        }
    }


}
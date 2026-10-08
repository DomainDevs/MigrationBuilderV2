using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.Configuration;
using Persistence.Configuration;
using Persistence.Execution.Services;
using System.Text;

namespace Persistence.Execution.Services;


public sealed class SqlProcessExecutor
{
    private readonly IDatabaseContext _database;
    private readonly ConnectionConfig _targetConfig;

    public SqlProcessExecutor(
        IDatabaseContext database,
        IConfiguration configuration)
    {
        _database = database;

        _targetConfig =
            configuration
                .GetSection("Connections:Target")
                .Get<ConnectionConfig>()
            ?? throw new InvalidOperationException(
                "No se encontró la configuración Connections:Target.");
    }

    public async Task ExecuteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"El directorio '{path}' no existe.");
        }

        FileInfo[] files =
            new DirectoryInfo(path)
                .GetFiles(
                    "*.sql",
                    SearchOption.TopDirectoryOnly);

        List<(FileInfo File, long Order)> orderedFiles = [];

        foreach (FileInfo file in files)
        {
            string fileName =
                Path.GetFileNameWithoutExtension(
                    file.Name);

            int separatorIndex =
                fileName.IndexOf(
                    '_',
                    StringComparison.Ordinal);

            string numberText =
                separatorIndex > 0
                    ? fileName[..separatorIndex]
                    : fileName;

            if (!long.TryParse(
                    numberText,
                    out long order))
            {
                throw new InvalidOperationException(
                    $"El archivo '{file.Name}' no tiene una numeración válida al inicio del nombre.");
            }

            orderedFiles.Add(
                (file, order));
        }

        IEnumerable<(FileInfo File, long Order)> processes =
            orderedFiles
                .OrderBy(x => x.Order)
                .ThenBy(
                    x => x.File.Name,
                    StringComparer.OrdinalIgnoreCase);

        using IUnitOfWork target =
            _database["Target"].CreateNew();

        foreach (
            (FileInfo File, long Order) process
            in processes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sql =
                await File.ReadAllTextAsync(
                    process.File.FullName,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(sql))
                continue;

            try
            {
                foreach (string batch in SplitBatches(sql))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    await target.Sql.ExecuteAsync(
                        batch,
                        commandTimeout: _targetConfig.TimeOut);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error ejecutando el proceso '{process.File.Name}' en '{path}'.",
                    ex);
            }
        }
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
}
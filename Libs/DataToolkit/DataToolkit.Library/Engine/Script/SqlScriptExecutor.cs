using DataToolkit.Library.Engine.Abstractions;
using DataToolkit.Library.UnitOfWorkLayer;
using System.Text;

namespace DataToolkit.Library.Engine.Script;

public sealed class SqlScriptExecutor : ISqlScriptExecutor
{
    public async Task ExecuteAsync(
        IUnitOfWork unitOfWork,
        string script,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }

        foreach (var batch in Split(script))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await unitOfWork.Sql.ExecuteAsync(batch);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error executing SQL batch.",
                    ex);
            }
        }
    }

    private static IEnumerable<string> Split(
        string script)
    {
        var batch = new StringBuilder();

        using var reader = new StringReader(script);

        while (reader.ReadLine() is string line)
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
}
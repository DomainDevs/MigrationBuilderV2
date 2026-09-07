using Persistence.Migration.Services;
using System.Text;

namespace Persistence.Planning.Services;

public sealed class CleanScriptGenerator
{
    public string Generate(MigrationPlan plan)
    {
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));

        StringBuilder script = new();

        foreach (MigrationPackage package in
                 plan.Packages
                     .Where(p => p.Enabled)
                     .OrderByDescending(p => p.Stage))
        {
            if (string.IsNullOrWhiteSpace(package.Package))
                continue;

            string[] parts =
                package.Package
                    .Split(
                        '.',
                        2,
                        StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                continue;

            string schema = parts[0];
            string table = parts[1];

            script.AppendLine(
                $"TRUNCATE TABLE [{schema}].[{table}];");

            script.AppendLine("GO");
            script.AppendLine();
        }

        return script.ToString().TrimEnd();
    }
}

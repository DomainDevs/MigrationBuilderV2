namespace Persistence.Execution.Helpers;

internal static class ArtifactDiscovery
{
    public static IReadOnlyList<string> Discover(string packagePath)
    {
        return Directory
            .GetFiles(packagePath, "*.sql")
            .Concat(Directory.EnumerateFiles(packagePath, "*.dtsx"))
            .OrderBy(GetOrder)
            .ToList();
    }

    private static int GetOrder(string file)
    {
        string name = Path.GetFileName(file);

        return name switch
        {
            var x when x.StartsWith("BEGIN_", StringComparison.OrdinalIgnoreCase) => 1,
            var x when x.StartsWith("DDL_", StringComparison.OrdinalIgnoreCase) => 2,
            var x when x.StartsWith("SQL_", StringComparison.OrdinalIgnoreCase) => 3,
            var x when x.StartsWith("LOAD_", StringComparison.OrdinalIgnoreCase) => 4,
            var x when x.StartsWith("END_", StringComparison.OrdinalIgnoreCase) => 5,
            var x when x.StartsWith("ETL_", StringComparison.OrdinalIgnoreCase) => 6,
            _ => 99
        };
    }
}
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
            var x when x.EndsWith(".dtsx", StringComparison.OrdinalIgnoreCase) => 5,
            var x when x.StartsWith("END_", StringComparison.OrdinalIgnoreCase) => 6,
            _ => 99
        };
    }

    public static IReadOnlyList<string> DiscoverNumbered(
        string folderPath)
    {
        return Directory
            .GetFiles(folderPath, "*.sql")
            .Select(file => new
            {
                File = file,
                Order = GetNumericOrder(file)
            })
            .OrderBy(x => x.Order)
            .ThenBy(
                x => Path.GetFileName(x.File),
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.File)
            .ToList();
    }

    private static int GetNumericOrder(
        string file)
    {
        string name =
            Path.GetFileName(file);

        int separator =
            name.IndexOf('_');

        if (separator <= 0)
        {
            throw new InvalidOperationException(
                $"El archivo '{name}' no cumple la convención de numeración requerida. " +
                "Debe utilizar el formato NN_Nombre.sql.");
        }

        string prefix =
            name[..separator];

        if (!int.TryParse(
                prefix,
                out int order))
        {
            throw new InvalidOperationException(
                $"El archivo '{name}' no contiene una numeración válida.");
        }

        return order;
    }
}
using DataToolkit.Library;

namespace Persistence.Migration.Metadata;

internal static class MetadataNormalizer
{
    public static List<TableMetadata> NormalizeMetadata(
    IEnumerable<TableMetadata> metadata)
    {
        return metadata
            .GroupBy(
                x => $"{x.Schema}.{x.Name}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public static List<TableMetadata> NormalizeColumns(
        IEnumerable<TableMetadata> metadata)
    {
        List<TableMetadata> tables = metadata.ToList();

        foreach (TableMetadata table in tables)
        {
            table.Columns = table.Columns
                .GroupBy(
                    x => x.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
        }

        return tables;
    }

}

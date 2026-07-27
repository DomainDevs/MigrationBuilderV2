using DataToolkit.Library;
using DataToolkit.Library.UnitOfWorkLayer;
using Persistence.Metadata.Services;

namespace Persistence.Planning.Services;

public sealed class DependencyResolverService
{
    private readonly MetadataService _metadataService;

    public DependencyResolverService(
        MetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    public async Task<List<string>> ResolveDependenciesAsync(
        //IUnitOfWork source,
        string? schema,
        List<string>? tables)
    {
        HashSet<string> result =
            new(
                tables ?? [],
                StringComparer.OrdinalIgnoreCase);

        bool hasChanges;

        do
        {
            hasChanges = false;

            List<TableMetadata> metadata =
                await _metadataService.ExtractMetadataAsync(
                    true,
                    //source,
                    schema,
                    result.ToList());

            foreach (TableMetadata table in metadata)
            {
                foreach (ColumnMetadata column in table.Columns)
                {
                    if (string.IsNullOrWhiteSpace(column.ForeignTable))
                    {
                        continue;
                    }

                    if (result.Add(column.ForeignTable))
                    {
                        hasChanges = true;
                    }
                }
            }

        } while (hasChanges);

        return result
            .OrderBy(x => x)
            .ToList();
    }
}
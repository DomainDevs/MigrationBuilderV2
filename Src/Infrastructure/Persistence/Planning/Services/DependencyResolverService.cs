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
        // 1. Caso base: Si no se enviaron tablas específicas, no hay nada que resolver
        if (tables is null || tables.Count == 0)
        {
            return [];
        }

        // 2. Traer la metadata completa del esquema en UNA SOLA llamada I/O (en lugar de N llamadas en bucle)
        List<TableMetadata> allMetadata = await _metadataService.ExtractMetadataAsync(
            "Target",
            schema,
            tables: null); // Trae el mapa completo del esquema

        // Indexar por nombre de tabla para búsquedas O(1)
        var metadataLookup = allMetadata.ToDictionary(
            t => t.Name,
            StringComparer.OrdinalIgnoreCase);

        var resolvedTables = new HashSet<string>(tables, StringComparer.OrdinalIgnoreCase);
        var processingQueue = new Queue<string>(tables);

        // 3. Recorrido del grafo en memoria mediante BFS (Breadth-First Search)
        while (processingQueue.Count > 0)
        {
            string currentTableName = processingQueue.Dequeue();

            if (!metadataLookup.TryGetValue(currentTableName, out var tableMetadata))
            {
                continue;
            }

            foreach (ColumnMetadata column in tableMetadata.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.ForeignTable))
                {
                    continue;
                }

                // Evitar self-references en la cola de exploración
                if (string.Equals(currentTableName, column.ForeignTable, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Si la tabla foránea es nueva, la registramos y la ponemos en cola para explorar sus propias FKs
                if (resolvedTables.Add(column.ForeignTable))
                {
                    processingQueue.Enqueue(column.ForeignTable);
                }
            }
        }

        return resolvedTables
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
using DataToolkit.Library;
using DataToolkit.Library.UnitOfWorkLayer;
using Persistence.Metadata.Services;

namespace Persistence.Planning.Services;

public sealed class MigrationPlanningService
{
    private readonly MetadataService _metadataService;
    private readonly DependencyAnalyzer _dependencyAnalyzer;

    public MigrationPlanningService(
        MetadataService metadataService,
        DependencyAnalyzer dependencyAnalyzer)
    {
        _metadataService = metadataService;
        _dependencyAnalyzer = dependencyAnalyzer;
    }

    public async Task<IReadOnlyList<TableMetadata>> BuildExecutionPlanAsync(
        IUnitOfWork source,
        string? schema,
        List<string>? tables)
    {
        var metadata = await _metadataService.ExtractMetadataAsync(
            "Source",   //source,
            schema,
            tables);

        return _dependencyAnalyzer.Sort(metadata);
    }

    public async Task<IReadOnlyList<string>> BuildExecutionPlanStringAsyncStr(
        IUnitOfWork source,
        string? schema,
        List<string>? tables)
    {
        var metadata = await _metadataService.ExtractMetadataAsync(
            "Target", //source,
            schema,
            tables);

        return _dependencyAnalyzer.SortToString(metadata);
    }
}

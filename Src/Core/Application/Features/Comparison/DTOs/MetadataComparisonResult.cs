
namespace Application.Features.Comparison.DTOs;

public sealed class MetadataComparisonResult
{
    public MetadataComparisonSummary Summary { get; set; } = new();
    public List<TableComparisonResult> Tables { get; set; } = [];
}

namespace Application.Features.Comparison.DTOs;

public sealed class MetadataComparisonResult
{
    public long ElapsedMilliseconds { get; set; }
    public MetadataComparisonSummary Summary { get; set; } = new();
    public List<TableComparisonResult> Tables { get; set; } = [];
}
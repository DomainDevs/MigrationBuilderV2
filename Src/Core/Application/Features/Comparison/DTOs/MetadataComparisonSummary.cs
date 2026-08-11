namespace Application.Features.Comparison.DTOs;

public sealed class MetadataComparisonSummary
{
    public int TablesCompared { get; set; }
    public int TablesWithDifferences { get; set; }
    public int TablesOnlyInSource { get; set; }
    public int TablesOnlyInTarget { get; set; }
    public int ColumnsAdded { get; set; }
    public int ColumnsRemoved { get; set; }
    public int ColumnsChanged { get; set; }
}
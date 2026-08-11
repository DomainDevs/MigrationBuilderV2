namespace Application.Features.Comparison.DTOs;

public sealed class TableComparisonResult
{
    public string Table { get; set; } = "";
    public Dictionary<string, List<string>> Differences { get; set; } = [];
}
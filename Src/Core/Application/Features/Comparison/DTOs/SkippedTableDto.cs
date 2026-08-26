namespace Application.Features.Comparison.DTOs;

public sealed class SkippedTableDto
{
    public string Table { get; init; } = string.Empty;
    public long SourceRecords { get; init; }
    public long TargetRecords { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}


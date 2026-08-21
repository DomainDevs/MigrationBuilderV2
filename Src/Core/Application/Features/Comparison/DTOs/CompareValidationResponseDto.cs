namespace Application.Features.Comparison.DTOs;

public sealed class CompareValidationResponseDto
{
    public string Source { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public int Tables { get; init; }
    public int Validated { get; init; }
    public int WithDifferences { get; init; }
    public long ElapsedMilliseconds { get; init; }
    public string ElapsedTime { get; init; } = string.Empty;
    public List<string> Differences { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}
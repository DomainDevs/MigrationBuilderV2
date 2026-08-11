namespace Application.Features.Comparison.Commands;

public sealed class CompareMetadataCommand
{
    public required string Source { get; set; }
    public required string Target { get; set; }
    public string? Schema { get; set; }
    public List<string>? Tables { get; set; }
}
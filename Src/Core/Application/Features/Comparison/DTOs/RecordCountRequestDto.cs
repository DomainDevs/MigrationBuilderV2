using System.ComponentModel;

namespace Application.Features.Comparison.DTOs;

public sealed class RecordCountRequestDto
{
    [DefaultValue("Source")]
    public required string Source { get; set; } = "Source";

    [DefaultValue("Target")]
    public required string Target { get; set; } = "Target";

    [DefaultValue("dbo")]
    public string? Schema { get; set; } = "dbo";

    [DefaultValue("[]")]
    public List<string> Tables { get; set; } = [];
}
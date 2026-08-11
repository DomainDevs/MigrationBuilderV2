using System.ComponentModel;

namespace Application.Features.Comparison.DTOs;

public sealed class MetadataRequestDto
{
    [DefaultValue("Target")]
    public required string Source { get; set; } = "Target";

    [DefaultValue("Knowledge")]
    public required string Target { get; set; } = "Knowledge";

    [DefaultValue("dbo")]
    public string? Schema { get; set; } = "dbo";
    [DefaultValue("[]")]
    public List<string>? Tables { get; set; }
}
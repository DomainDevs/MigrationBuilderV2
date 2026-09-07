using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Application.Features.Comparison.DTOs;

public sealed class RecordValueRequestDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DefaultValue("")]
    public string? ProjectName { get; set; }

    [DefaultValue("Source")]
    public required string Source { get; set; } = "Source";

    [DefaultValue("Target")]
    public required string Target { get; set; } = "Target";

    [DefaultValue("dbo")]
    public string? Schema { get; set; } = "dbo";

    [DefaultValue("[]")]
    public List<string> Tables { get; set; } = [];
}

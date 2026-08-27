using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Application.Features.Comparison.DTOs;

public sealed class RecordCountRequestDto
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
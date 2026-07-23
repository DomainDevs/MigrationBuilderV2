using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.Features.Project.DTOs;

public sealed class ProjectCreateRequestDto
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

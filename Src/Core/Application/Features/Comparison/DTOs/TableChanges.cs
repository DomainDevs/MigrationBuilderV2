
namespace Application.Features.Comparison.DTOs;

public sealed class TableChanges
{
    public List<string> Added { get; set; } = [];
    public List<string> Removed { get; set; } = [];
    public Dictionary<string, Dictionary<string, object[]>> Modified { get; set; } = [];
}

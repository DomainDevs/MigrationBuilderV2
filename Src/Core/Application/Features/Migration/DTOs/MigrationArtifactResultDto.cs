namespace Application.Features.Migration.DTOs;

public sealed class MigrationArtifactResultDto
{
    public string Artifact { get; init; } = string.Empty;
    public int GeneratedFiles { get; init; }
    public int SkippedFiles { get; init; }
    public List<string> Files { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

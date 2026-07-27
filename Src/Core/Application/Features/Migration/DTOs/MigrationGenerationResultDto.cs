namespace Application.Features.Migration.DTOs;

public sealed class MigrationGenerationResultDto
{
    public List<MigrationArtifactResultDto> Artifacts { get; init; } = [];

    public int TotalGeneratedFiles => Artifacts.Sum(a => a.GeneratedFiles);
    public int TotalSkippedFiles => Artifacts.Sum(a => a.SkippedFiles);
    public List<string> Warnings => Artifacts.SelectMany(a => a.Warnings).ToList();
}

namespace Application.Features.Orchestrator.DTOs;

public sealed record MigrationExecuteResponse
{
    public required IReadOnlyList<PackageExecutionDto> Packages { get; init; }
}
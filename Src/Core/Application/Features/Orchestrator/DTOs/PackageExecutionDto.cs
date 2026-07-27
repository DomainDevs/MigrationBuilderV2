namespace Application.Features.Orchestrator.DTOs;

public sealed record PackageExecutionDto
{
    public required string Package { get; init; }

    public required string Status { get; init; }
}
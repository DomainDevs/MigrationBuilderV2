namespace Application.Features.Orchestrator.DTOs.Responses;

public sealed record PackageExecutionResponse
{
    public required string Package { get; init; }

    public required string Status { get; init; }
}
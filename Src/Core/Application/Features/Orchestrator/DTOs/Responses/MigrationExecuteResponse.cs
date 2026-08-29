namespace Application.Features.Orchestrator.DTOs.Responses;

public sealed record MigrationExecuteResponse
{
    public required IReadOnlyList<PackageExecutionResponse> Packages { get; init; }
}
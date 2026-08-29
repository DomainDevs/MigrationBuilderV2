namespace Application.Features.Orchestrator.Commands;

public sealed record MigrationParameter
{
    public required string Name { get; init; }

    public string? Value { get; init; }
}


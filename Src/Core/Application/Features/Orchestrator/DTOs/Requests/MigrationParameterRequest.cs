using Domain.Enums;
using System.ComponentModel;

namespace Application.Features.Orchestrator.DTOs.Requests;

public sealed record MigrationParameterRequest
{
    [DefaultValue("")]
    public required string Name { get; init; }
    [DefaultValue("")]
    public string? Value { get; init; }
    public required MigrationParameterType Type { get; init; }
}


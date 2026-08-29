using System.ComponentModel;

namespace Application.Features.Orchestrator.DTOs.Requests;

public sealed record MigrationExecuteRequest
{
    [DefaultValue("Master")]
    public required string ProjectName { get; init; }

    //[DefaultValue("[]")]
    public List<MigrationParameterRequest> Parameters { get; init; } = [];

    [DefaultValue("[]")]
    public List<string> Packages { get; init; } = [];
}


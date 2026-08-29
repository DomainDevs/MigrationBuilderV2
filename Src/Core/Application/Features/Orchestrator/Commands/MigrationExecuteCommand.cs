using Application.Features.Orchestrator.DTOs.Responses;
using MediatR;
using System.ComponentModel;

namespace Application.Features.Orchestrator.Commands;

public sealed record MigrationExecuteCommand
    : IRequest<MigrationExecuteResponse>
{
    [DefaultValue("Master")]
    public required string ProjectName { get; init; } = string.Empty;

    public List<MigrationParameter> Parameters { get; init; } = [];

    public List<string> Packages { get; init; } = [];

    
}
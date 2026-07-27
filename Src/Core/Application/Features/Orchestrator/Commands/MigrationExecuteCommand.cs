using Application.Features.Orchestrator.DTOs;
using MediatR;
using System.ComponentModel;

namespace Application.Features.Orchestrator.Commands;

public sealed record MigrationExecuteCommand
    : IRequest<MigrationExecuteResponse>
{
    [DefaultValue("Master")]
    public required string ProjectName { get; init; } = string.Empty;
    public List<string> Packages { get; init; } = [];
}
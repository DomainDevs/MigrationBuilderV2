using Application.Features.Orchestrator.DTOs;
using MediatR;

namespace Application.Features.Orchestrator.Commands;

public sealed record MigrationExecuteCommand
    : IRequest<MigrationExecuteResponse>
{
    public required string ProjectName { get; init; }
    public List<string> Packages { get; init; } = [];
}
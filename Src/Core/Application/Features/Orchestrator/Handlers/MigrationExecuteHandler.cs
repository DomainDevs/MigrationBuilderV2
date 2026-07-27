using Application.Abstractions.Execution;
using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs;
using MediatR;

namespace Application.Features.Orchestrator.Handlers;

public sealed class MigrationExecuteHandler
    : IRequestHandler<MigrationExecuteCommand, MigrationExecuteResponse>
{
    private readonly IMigrationExecutor _migrationExecutor;

    public MigrationExecuteHandler(
        IMigrationExecutor migrationExecutor)
    {
        _migrationExecutor = migrationExecutor;
    }

    public Task<MigrationExecuteResponse> Handle(
        MigrationExecuteCommand request,
        CancellationToken cancellationToken)
    {
        return _migrationExecutor.ExecuteAsync(request.ProjectName);
    }
}
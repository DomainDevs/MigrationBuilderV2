using Application.Abstractions.Execution;
using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs.Responses;
using MediatR;

namespace Application.Features.Orchestrator.Handlers;

public sealed class MigrationExecuteHandler
    : IRequestHandler<MigrationExecuteCommand, MigrationExecuteResponse>
{
    private readonly IMigrationExecutor _migrationExecutor;
    private readonly IMigrationExecutionGuard _guard;

    public MigrationExecuteHandler(
        IMigrationExecutionGuard guard,
        IMigrationExecutor migrationExecutor)
    {
        _guard = guard;
        _migrationExecutor = migrationExecutor;
    }

    public async Task<MigrationExecuteResponse> Handle(
        MigrationExecuteCommand request,
        CancellationToken cancellationToken)
    {
        if (!_guard.TryEnter())
            throw new InvalidOperationException(
                "Ya existe una migración en ejecución.");

        try
        {
            return await _migrationExecutor.ExecuteAsync(
                request,
                cancellationToken);
        }
        finally
        {
            _guard.Exit();
        }
    }
}
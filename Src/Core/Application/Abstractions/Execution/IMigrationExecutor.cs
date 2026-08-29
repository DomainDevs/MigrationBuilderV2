using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs.Responses;

namespace Application.Abstractions.Execution;

public interface IMigrationExecutor
{
    Task<MigrationExecuteResponse> ExecuteAsync(
        MigrationExecuteCommand command,
        CancellationToken cancellationToken);
}
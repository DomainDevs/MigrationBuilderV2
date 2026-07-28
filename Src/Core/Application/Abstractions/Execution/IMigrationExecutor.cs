using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs;

namespace Application.Abstractions.Execution;

public interface IMigrationExecutor
{
    Task<MigrationExecuteResponse> ExecuteAsync(MigrationExecuteCommand command);
}

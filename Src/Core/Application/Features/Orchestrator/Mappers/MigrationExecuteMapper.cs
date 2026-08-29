using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs.Requests;
using Riok.Mapperly.Abstractions;

namespace Application.Features.Orchestrator.Mappers;

[Mapper]
public static partial class MigrationExecuteMapper
{
    public static partial MigrationExecuteCommand ToCommand(
        this MigrationExecuteRequest request);

    private static partial MigrationParameter ToParameter(
        this MigrationParameterRequest request);
}
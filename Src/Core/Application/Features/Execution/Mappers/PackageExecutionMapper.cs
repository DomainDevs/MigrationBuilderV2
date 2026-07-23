// =========================================================
// Este mapper fue generado automáticamente por DataToolkit.
// Usa Riok.Mapperly para generar las implementaciones en build.
// =========================================================

using Application.Features.Execution.DTOs;
using Application.Features.Execution.Commands;
using Riok.Mapperly.Abstractions;
using Entities = Domain.Entities.Workspace;
//using Application.Features.Execution.Commands;

namespace Application.Features.Execution.Mappers;

[Mapper(AllowNullPropertyAssignment = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class PackageExecutionMapper
{
    // DTO → Commands
    public static partial PackageExecutionUpdateCommand ToUpdateCommand(this PackageExecutionUpdateRequestDto dto);
    public static partial PackageExecutionCreateCommand ToCommandCreate(this PackageExecutionCreateRequestDto dto);

    // Commands → Entity
    public static partial Entities.PackageExecution ToEntity(PackageExecutionCreateCommand command);
    public static partial Entities.PackageExecution ToEntity(PackageExecutionUpdateCommand command);

    // Entity → DTO
    public static partial PackageExecutionQueryResponseDto ToDto(Entities.PackageExecution entity);

}
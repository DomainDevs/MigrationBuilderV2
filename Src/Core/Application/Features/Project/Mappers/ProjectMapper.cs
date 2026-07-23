using Application.Features.Project.Commands;
using Application.Features.Project.DTOs;
using Riok.Mapperly.Abstractions;

namespace Application.Features.Project.Mappers;

[Mapper(AllowNullPropertyAssignment = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ProjectMapper
{
    
    // DTO → Commands
    public static partial ProjectCreateCommand ToCommandCreate(this ProjectCreateRequestDto dto);
}

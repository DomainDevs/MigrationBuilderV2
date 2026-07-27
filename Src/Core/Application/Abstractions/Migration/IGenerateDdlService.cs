using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;

namespace Application.Abstractions.Migration;

public interface IGenerateDdlService
{
    Task<MigrationResponseDto> GenerateDdlScriptsAsync(GenerateDdlCommand generateDdlCommand);
}
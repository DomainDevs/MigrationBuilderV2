using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;

namespace Application.Abstractions.Migration;

public interface IGenerateExtractionService
{
    Task<MigrationResponseDto> GenerateExtractionAsync(GenerateExtractionCommand generateExtractionCommand);
}
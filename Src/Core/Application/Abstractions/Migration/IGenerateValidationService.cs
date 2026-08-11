using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;

namespace Application.Abstractions.Migration;

public interface IGenerateValidationService
{
    Task<MigrationResponseDto> GenerateValidationAsync(GenerateValidationCommand command);
}
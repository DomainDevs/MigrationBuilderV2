using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;

namespace Application.Abstractions.Migration;

public interface IGeneratePlanService
{
    Task<MigrationGenerationResultDto> GenerateMigrationPlanAsync(GeneratePlanCommand command);
}
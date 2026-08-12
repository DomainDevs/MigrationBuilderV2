using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using Application.Features.Migration.Commands;

namespace Application.Abstractions.Comparison;

public interface ICompareValidationService
{
    Task<CompareValidationResponseDto> CompareValidationAsync(CompareValidationCommand command);
}

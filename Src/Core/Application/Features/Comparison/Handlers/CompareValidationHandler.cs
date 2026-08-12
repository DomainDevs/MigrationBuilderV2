using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;

namespace Application.Features.Comparison.Handlers;

public sealed class CompareValidationHandler
{
    private readonly ICompareValidationService _comparisonService;

    public CompareValidationHandler(
        ICompareValidationService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    public async Task<CompareValidationResponseDto> HandleAsync(
        CompareValidationCommand command)
    {
        return await _comparisonService.CompareValidationAsync(command);
    }
}
using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;

namespace Application.Features.Comparison.Handlers;

public sealed class CompareMetadataHandler
{
    private readonly IMetadataComparisonService _comparisonService;

    public CompareMetadataHandler(
        IMetadataComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    public async Task<MetadataComparisonResult> HandleAsync(
        CompareMetadataCommand command)
    {
        return await _comparisonService.CompareAsync(command);
    }
}
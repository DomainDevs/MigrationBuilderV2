using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;

namespace Application.Abstractions.Comparison;

public interface IMetadataComparisonService
{
    Task<MetadataComparisonResult> CompareAsync(
        CompareMetadataCommand command);
}
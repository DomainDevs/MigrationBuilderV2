using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using Riok.Mapperly.Abstractions;

namespace Application.Features.Comparison.Mappers;

[Mapper]
public static partial class MetadataMapper
{
    // DTO → Command
    public static partial CompareMetadataCommand ToCompareMetadataCommand(
        this MetadataRequestDto dto);
}
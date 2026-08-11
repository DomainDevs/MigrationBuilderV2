using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using Riok.Mapperly.Abstractions;

namespace Application.Features.Migration.Mappers;

[Mapper]
public static partial class MigrationMapper
{
    // DTO → Commands
    public static partial GeneratePlanCommand ToGeneratePlanCommand(this MigrationRequestDto dto);

    public static partial GenerateDdlCommand ToGenerateDdlCommand(this MigrationRequestDto dto);

    public static partial GenerateExtractionCommand ToGenerateExtractionCommand(this MigrationRequestDto dto);

    public static partial GenerateLoadCommand ToGenerateLoadCommand(this MigrationRequestDto dto);

    public static partial GenerateValidationCommand ToGenerateValidationCommand(this MigrationComparisonRequestDto dto);
}
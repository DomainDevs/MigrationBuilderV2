using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;

namespace Application.Abstractions.Migration;

public interface IGenerateLoadService
{
    Task<MigrationResponseDto> GenerateLoadAsync(GenerateLoadCommand generateLoadCommand);
}
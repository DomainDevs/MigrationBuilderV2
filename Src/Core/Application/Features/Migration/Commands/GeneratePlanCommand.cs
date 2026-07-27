using Application.Features.Migration.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Features.Migration.Commands;

/*
public sealed record GeneratePlanCommand (
    string ProjectName,
    string? Schema,
    ArtifactType ArtifactType,
    List<string> Tables)
    : IRequest<MigrationResponseDto>;
*/

public sealed record GeneratePlanCommand(
    string ProjectName,
    string? Schema,
    ArtifactType ArtifactType,
    List<string> Tables)
    : IRequest<MigrationGenerationResultDto>;
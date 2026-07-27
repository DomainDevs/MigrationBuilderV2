using Application.Features.Migration.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Features.Migration.Commands;

public sealed record GenerateDdlCommand(
    string ProjectName,
    string? Schema,
    ArtifactType ArtifactType,
    List<string> Tables)
    : IRequest<MigrationResponseDto>;
using Application.Features.Migration.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.Features.Migration.Commands;

public sealed record GenerateLoadCommand(
    string ProjectName,
    string? Schema,
    ArtifactType ArtifactType,
    List<string> Tables)
    : IRequest<MigrationResponseDto>;
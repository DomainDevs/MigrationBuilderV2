using Domain.Enums;
using MediatR;

namespace Application.Features.Migration.Commands;

public sealed record GenerateExtractionCommand(
    string? Schema,
    ArtifactType ArtifactType,
    List<string> Tables)
    : IRequest<string>;
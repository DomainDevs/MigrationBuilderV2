using Domain.Enums;
using MediatR;

namespace Application.Features.Migration.Commands;

public sealed record GeneratePlanCommand (
    string? Schema,
    ArtifactType ArtifactType,
    List<string> Tables)
    : IRequest<string>;

using Application.Features.Comparison.DTOs;
using MediatR;

namespace Application.Features.Comparison.Commands;
public sealed record CompareValidationCommand(
    string Source,
    string Target,
    string? Schema,
    List<string> Tables)
    : IRequest<CompareValidationResponseDto>;
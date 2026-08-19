using Application.Features.Comparison.DTOs;
using MediatR;

namespace Application.Features.Comparison.Commands;

public sealed record CompareRecordCountCommand(
    string Source,
    string Target,
    string? Schema,
    List<string> Tables)
    : IRequest<List<RecordCountResultDto>>;
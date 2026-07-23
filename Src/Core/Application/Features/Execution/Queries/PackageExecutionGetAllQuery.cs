// PackageExecutionGetAllQuery.cs
using MediatR;
using Application.Features.Execution.DTOs;

namespace Application.Features.Execution.Queries;

public record PackageExecutionGetAllQuery() : IRequest<IEnumerable<PackageExecutionQueryResponseDto>>;

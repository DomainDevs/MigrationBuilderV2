// PackageExecutionGetByIdQuery.cs
using MediatR;
using Application.Features.Execution.DTOs;

namespace Application.Features.Execution.Queries;

public record PackageExecutionGetByIdQuery(int ExecutionId) : IRequest<PackageExecutionQueryResponseDto?>;

// PackageExecutionGetAllHandler.cs
using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.Workspace;
using Application.Features.Execution.DTOs;
using Application.Features.Execution.Mappers;
using Application.Features.Execution.Queries;
using MediatR;
using Entities = Domain.Entities;

namespace Application.Features.Execution.Handlers;

public class PackageExecutionGetAllHandler : IRequestHandler<PackageExecutionGetAllQuery, IEnumerable<PackageExecutionQueryResponseDto>>
{
    private readonly IPackageExecutionRepository _repo;
    public PackageExecutionGetAllHandler(IPackageExecutionRepository repo) => _repo = repo;

    public async Task<IEnumerable<PackageExecutionQueryResponseDto>> Handle(PackageExecutionGetAllQuery request, CancellationToken ct)
        => (await _repo.GetAllAsync()).Select(entity => PackageExecutionMapper.ToDto(entity));
}
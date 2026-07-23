// PackageExecutionGetByIdHandler.cs
using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.Workspace;
using Application.Features.Execution.DTOs;
using Application.Features.Execution.Mappers;
using Application.Features.Execution.Queries;
using MediatR;
using Entities = Domain.Entities;

namespace Application.Features.Execution.Handlers;

public class PackageExecutionGetByIdHandler : IRequestHandler<PackageExecutionGetByIdQuery, PackageExecutionQueryResponseDto?>
{
    private readonly IPackageExecutionRepository _repo;
    public PackageExecutionGetByIdHandler(IPackageExecutionRepository repo) => _repo = repo;

    public async Task<PackageExecutionQueryResponseDto?> Handle(PackageExecutionGetByIdQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.ExecutionId);
        if (entity == null) return null;
        return PackageExecutionMapper.ToDto(entity);
    }
}
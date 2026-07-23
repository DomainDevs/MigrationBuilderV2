// PackageExecutionCreateHandler.cs
using Application.Abstractions.Persistence.Workspace;
using Application.Features.Execution.Commands;
using Application.Features.Execution.Mappers;
using MediatR;
//using Entities = Domain.Entities;

namespace Application.Features.Execution.Handlers;

public class PackageExecutionCreateHandler : IRequestHandler<PackageExecutionCreateCommand, int>
{
    private readonly IPackageExecutionRepository _repo;
    public PackageExecutionCreateHandler(IPackageExecutionRepository repo) => _repo = repo;

    public async Task<int> Handle(PackageExecutionCreateCommand request, CancellationToken ct)
    {
        var entity = PackageExecutionMapper.ToEntity(request);
        return await _repo.InsertAsync(entity);
    }
}
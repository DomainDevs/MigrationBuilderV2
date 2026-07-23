using DataToolkit.Library.Repositories;
using Persistence.Connect.Context;
using System.Linq.Expressions;
using Domain.Entities.Workspace;
using Application.Abstractions.Persistence.Workspace;

namespace Persistence.Repositories.Workspace;

public sealed class PackageExecutionRepository : IPackageExecutionRepository
{
    private readonly IGenericRepository<PackageExecution> _repository;

    public PackageExecutionRepository(SqliteContext context)
    {
        _repository = context.Workspace.Repository<PackageExecution>();
    }

    public Task<int> InsertAsync(PackageExecution entity)
    {
        return _repository.InsertAsync(entity);
    }

    public Task<int> UpdateAsync(
        PackageExecution entity,
        params Expression<Func<PackageExecution, object>>[] includeProperties)
    {
        return _repository.UpdateAsync(entity, includeProperties);
    }

    public Task<IEnumerable<PackageExecution>> GetAllAsync(
        params Expression<Func<PackageExecution, object>>[]? selectProperties)
    {
        return _repository.GetAllAsync(selectProperties);
    }

    public Task<PackageExecution?> GetByIdAsync(
        int executionId,
        params Expression<Func<PackageExecution, object>>[]? selectProperties)
    {
        var entity = new PackageExecution
        {
            ExecutionId = executionId
        };

        return _repository.GetByIdAsync(entity, selectProperties);
    }

    public Task<int> DeleteByIdAsync(int executionId)
    {
        var entity = new PackageExecution
        {
            ExecutionId = executionId
        };

        return _repository.DeleteAsync(entity);
    }
}
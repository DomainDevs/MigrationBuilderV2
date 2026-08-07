using Application.Abstractions.Persistence.Workspace;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.Repositories;
using Domain.Entities.Workspace;
using Persistence.Connect.Context;
using System.Linq.Expressions;

namespace Persistence.Workspace.Repositories;

public sealed class PackageExecutionRepository : IPackageExecutionRepository
{
    private readonly IGenericRepository<PackageExecution> _repository;
    private readonly IDatabaseContext _database;

    public PackageExecutionRepository(
        IDatabaseContext database //SqliteContext context
        )
    {
        _database = database;
        _repository = _database["Workspace"].Repository<PackageExecution>();
        //_repository = context.Workspace.Repository<PackageExecution>();
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
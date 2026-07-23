using Domain.Entities.Workspace;
using System.Linq.Expressions;

namespace Application.Abstractions.Persistence.Workspace;

public interface IPackageExecutionRepository
{
    Task<int> DeleteByIdAsync(int executionId);
    Task<IEnumerable<PackageExecution>> GetAllAsync(params Expression<Func<PackageExecution, object>>[]? selectProperties);
    Task<PackageExecution?> GetByIdAsync(int executionId, params Expression<Func<PackageExecution, object>>[]? selectProperties);
    Task<int> InsertAsync(PackageExecution entity);
    Task<int> UpdateAsync(PackageExecution entity, params Expression<Func<PackageExecution, object>>[] includeProperties);
}
using System.Linq.Expressions;

namespace DataToolkit.Library.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<int> DeleteAsync(T entity);
        Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string sp, object parameters);
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string sp, object parameters);
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[]? selectProperties);
        Task<T?> GetByIdAsync(T entity, params Expression<Func<T, object>>[]? selectProperties);
        Task<int> InsertAsync(T entity);
        Task<int> UpdateAsync(T entity, params Expression<Func<T, object>>[] includeProperties);
    }
}
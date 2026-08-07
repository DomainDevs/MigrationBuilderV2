using Domain.Entities.Authentication;
using System.Linq.Expressions;

namespace Application.Abstractions.Persistence.Authentication;

public interface IUserRepository
{
    Task<int> DeleteByIdAsync(int userId);
    Task<IEnumerable<User>> GetAllAsync(params Expression<Func<User, object>>[]? selectProperties);
    Task<User?> GetByIdAsync(int userId, params Expression<Func<User, object>>[]? selectProperties);
    Task<int> InsertAsync(User entity);
    Task<int> UpdateAsync(User entity, params Expression<Func<User, object>>[] includeProperties);
}

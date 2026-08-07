using Application.Abstractions.Persistence.Authentication;
using DataToolkit.Library.Connections.Context;
using DataToolkit.Library.Repositories;
using Domain.Entities.Authentication;
using Persistence.Connect.Context;
using System.Linq.Expressions;

namespace Persistence.Authentication.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IGenericRepository<User> _repository;
    private readonly IDatabaseContext _database;

    public UserRepository(
        IDatabaseContext database //SqliteContext context
        )
    {
        //_repository = context.Workspace.Repository<User>();
        _database = database;
        _repository = _database["Workspace"].Repository<User>();
    }

    public Task<int> InsertAsync(User entity)
    {
        return _repository.InsertAsync(entity);
    }

    public Task<int> UpdateAsync(
        User entity,
        params Expression<Func<User, object>>[] includeProperties)
    {
        return _repository.UpdateAsync(entity, includeProperties);
    }

    public Task<IEnumerable<User>> GetAllAsync(
        params Expression<Func<User, object>>[]? selectProperties)
    {
        return _repository.GetAllAsync(selectProperties);
    }

    public Task<User?> GetByIdAsync(
        int userId,
        params Expression<Func<User, object>>[]? selectProperties)
    {
        var entity = new User
        {
            IdUser = userId
        };

        return _repository.GetByIdAsync(entity, selectProperties);
    }

    public Task<int> DeleteByIdAsync(int userId)
    {
        var entity = new User
        {
            IdUser = userId
        };

        return _repository.DeleteAsync(entity);
    }
}
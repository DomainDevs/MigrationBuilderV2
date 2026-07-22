using DataToolkit.Library.Engine.Abstractions;
using DataToolkit.Library.Repositories;
using System.Data;

namespace DataToolkit.Library.UnitOfWorkLayer
{
    public interface IUnitOfWork
    {
        bool HasActiveTransaction { get; }
        ISqlExecutor Sql { get; }
        IDbTransaction? Transaction { get; }

        IGenericRepository<T> Repository<T>()
            where T : class;

        void BeginTransaction();
        void Commit();
        void Rollback();
        void Dispose();
        
    }
}
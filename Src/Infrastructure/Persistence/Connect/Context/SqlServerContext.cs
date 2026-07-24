using DataToolkit.Library.Connections;
using DataToolkit.Library.UnitOfWorkLayer;

namespace Persistence.Connect.Context;

public sealed class SqlServerContext
{
    public IUnitOfWork Source { get; }

    public IUnitOfWork Target { get; }

    public SqlServerContext(
        IDbConnectionFactory factory)
    {
        Source = new UnitOfWork(factory, "Source");
        Target = new UnitOfWork(factory, "Target");
    }
}
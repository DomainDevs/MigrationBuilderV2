using DataToolkit.Library.Connections;
using DataToolkit.Library.UnitOfWorkLayer;

namespace Persistence.Connect.Context;

public sealed class SqliteContext
{
    public IUnitOfWork Workspace { get; }
    public SqliteContext(
        IDbConnectionFactory factory)
    {
        Workspace = new UnitOfWork(factory, "Workspace");
    }

}
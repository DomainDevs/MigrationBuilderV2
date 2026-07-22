using DataToolkit.Library.UnitOfWorkLayer;

namespace DataToolkit.Library.Engine.Abstractions;

public interface ISqlScriptExecutor
{
    Task ExecuteAsync(
        IUnitOfWork unitOfWork,
        string script,
        CancellationToken cancellationToken = default);
}
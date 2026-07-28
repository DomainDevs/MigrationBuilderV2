using Application.Abstractions.Execution;

namespace Persistence.Execution.Services;

public sealed class MigrationExecutionGuard : IMigrationExecutionGuard
{
    private int _isRunning;

    public bool TryEnter()
    {
        return Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0;
    }

    public void Exit()
    {
        Interlocked.Exchange(ref _isRunning, 0);
    }

    //Define the service lifetime for DependencyAnalyzer 
    public static KeyValuePair<string, string>[] ConfigureServices() =>
    [
        new("Lifetime", "Singleton")
    ];
}

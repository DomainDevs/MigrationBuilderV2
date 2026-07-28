namespace Application.Abstractions.Execution;

public interface IMigrationExecutionGuard
{
    bool TryEnter();
    void Exit();
}

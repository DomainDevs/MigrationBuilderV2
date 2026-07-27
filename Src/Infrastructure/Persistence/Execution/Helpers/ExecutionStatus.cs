namespace Persistence.Execution.Helpers;

public enum ExecutionStatus
{
    Pending,

    Running,

    Cancelling,

    Cancelled,

    Completed,

    Failed
}
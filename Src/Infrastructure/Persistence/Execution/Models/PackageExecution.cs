using Persistence.Execution.Helpers;

namespace Persistence.Execution.Models;

public sealed class PackageExecution
{
    public string Package { get; init; } = string.Empty;

    public ExecutionStatus Status { get; set; }
        = ExecutionStatus.Pending;

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string? Error { get; set; }
}

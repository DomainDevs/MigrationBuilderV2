namespace Persistence.Execution.Models;
public sealed class MigrationExecution
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public List<PackageExecution> Packages { get; } = [];
}
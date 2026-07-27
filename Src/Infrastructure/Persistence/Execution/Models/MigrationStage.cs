using Persistence.Migration.Services;

namespace Persistence.Execution.Models;

public sealed class MigrationStage
{
    public int Stage { get; init; }

    public List<MigrationPackage> Packages { get; } = [];
}
using Persistence.Execution.Helpers;
using Persistence.Execution.Models;
using Persistence.Metadata.Services;
using Persistence.Migration.Services;

namespace Persistence.Execution.Services;

public sealed class PackageExecutor
{
    private readonly ArtifactExecutor _artifactExecutor;
    private readonly MigrationPlanService _migrationPlanService;

    public PackageExecutor(
        ArtifactExecutor artifactExecutor,
        MigrationPlanService migrationPlanService)
    {
        _artifactExecutor = artifactExecutor;
        _migrationPlanService = migrationPlanService;
    }

    public async Task ExecuteAsync(
        string projectPath,
        string packagePath,
        MigrationPlan plan,
        MigrationPackage package,
        PackageExecution execution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(execution);

        if (!Directory.Exists(packagePath))
        {
            throw new DirectoryNotFoundException(
                $"No se encontró el paquete '{packagePath}'.");
        }

        IReadOnlyList<string> artifacts =
            ArtifactDiscovery.Discover(packagePath);

        if (artifacts.Count == 0)
        {
            throw new InvalidOperationException(
                $"El paquete '{packagePath}' no contiene artefactos SQL.");
        }

        execution.Status = ExecutionStatus.Running;
        execution.StartedAt = DateTime.UtcNow;

        try
        {
            foreach (string artifact in artifacts)
            {
                if (execution.Status == ExecutionStatus.Cancelling)
                {
                    execution.Status = ExecutionStatus.Cancelled;
                    return;
                }

                await _artifactExecutor.ExecuteAsync(artifact);
            }

            execution.Status = ExecutionStatus.Completed;

            _migrationPlanService.SetApproved(
                plan,
                package.Package,
                true);
        }
        catch (Exception ex)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.Error = ex.Message;

            throw;
        }
        finally
        {
            execution.FinishedAt = DateTime.UtcNow;

            _migrationPlanService.Save(
                projectPath,
                plan);
        }
    }
}
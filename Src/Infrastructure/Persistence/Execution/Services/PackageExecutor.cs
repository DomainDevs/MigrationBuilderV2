using Persistence.Execution.Helpers;
using Persistence.Execution.Log;
using Persistence.Execution.Models;
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

    public async Task ExecuteAsyncPKG(
        string projectPath,
        string packagePath,
        MigrationPlan plan,
        MigrationPackage package,
        PackageExecution execution,
        List<LogEntry> logs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(execution);

        if (!Directory.Exists(packagePath))
        {
            execution.Status = ExecutionStatus.Failed;
            execution.StartedAt = DateTime.UtcNow;
            execution.FinishedAt = DateTime.UtcNow;

            string mensaje =
                $"No existe la carpeta del paquete '{packagePath}'. Se omite la ejecución.";

            execution.Error = mensaje;

            LogEntry log = new()
            {
                Name = package.Package,
                Start = execution.StartedAt.Value,
                End = execution.FinishedAt.Value,
                Ok = false,
                Msg = mensaje
            };

            execution.Logs.Add(log);
            logs.Add(log);

            plan.Revision++;

            int index = plan.Packages.FindIndex(x =>
                x.Package.Equals(
                    package.Package,
                    StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
                plan.Packages[index].Approved = false;

            _migrationPlanService.Save(projectPath, plan);

            return;
        }

        IReadOnlyList<string> artifacts =
            ArtifactDiscovery.Discover(packagePath);

        // Si tiene ETL, retiro los demás artefactos y ejecuto solo el ETL.
        if (package.SelfContainedEtl)
        {
            artifacts = artifacts
                .Where(a =>
                    a.EndsWith(".dtsx", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            if (artifacts.Any(a =>
                a.EndsWith(".dtsx", StringComparison.OrdinalIgnoreCase)))
            {
                artifacts = artifacts
                    .Where(a =>
                    {
                        string fileName = Path.GetFileName(a);

                        return fileName.StartsWith(
                                   "BEGIN_",
                                   StringComparison.OrdinalIgnoreCase)
                               || fileName.StartsWith(
                                   "DDL_",
                                   StringComparison.OrdinalIgnoreCase)
                               || fileName.StartsWith(
                                   "END_",
                                   StringComparison.OrdinalIgnoreCase)
                               || a.EndsWith(
                                   ".dtsx",
                                   StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
            }

            if (!artifacts.Any(a =>
                    Path.GetFileName(a).StartsWith(
                        "SQL_",
                        StringComparison.OrdinalIgnoreCase))
                && artifacts.Any(a =>
                    Path.GetFileName(a).StartsWith(
                        "LOCAL_",
                        StringComparison.OrdinalIgnoreCase)))
            {
                List<string> ordered = [];

                ordered.AddRange(artifacts.Where(a =>
                    Path.GetFileName(a).StartsWith(
                        "BEGIN_",
                        StringComparison.OrdinalIgnoreCase)));

                ordered.AddRange(artifacts.Where(a =>
                    Path.GetFileName(a).StartsWith(
                        "LOCAL_",
                        StringComparison.OrdinalIgnoreCase)));

                ordered.AddRange(artifacts.Where(a =>
                    Path.GetFileName(a).StartsWith(
                        "END_",
                        StringComparison.OrdinalIgnoreCase)));

                artifacts = ordered;
            }
        }

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
                LogEntry log = new()
                {
                    Name = Path.GetFileName(artifact),
                    Start = DateTime.UtcNow
                };

                try
                {
                    if (execution.Status == ExecutionStatus.Cancelling)
                    {
                        execution.Status = ExecutionStatus.Cancelled;
                        return;
                    }

                    await _artifactExecutor.ExecuteAsync(artifact);

                    log.Ok = true;
                    log.Msg = "OK";
                }
                catch (Exception ex)
                {
                    log.Ok = false;
                    log.Msg = ex.Message;

                    throw new InvalidOperationException(
                        $"Error ejecutando '{log.Name}': {ex.Message}",
                        ex);
                }
                finally
                {
                    log.End = DateTime.UtcNow;

                    // Log asociado al paquete: lo consumirá la Web.
                    execution.Logs.Add(log);

                    // Log global: lo utilizará LogWriterJSON.
                    logs.Add(log);
                }
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
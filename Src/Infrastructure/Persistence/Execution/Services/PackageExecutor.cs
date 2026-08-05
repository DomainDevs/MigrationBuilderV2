using Persistence.Execution.Helpers;
using Persistence.Execution.Log;
using Persistence.Execution.Models;
using Persistence.Metadata.Services;
using Persistence.Migration.Services;
using Serilog;

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

        //LogWriterJSON writer = new($"{projectPath}\\Logs\\"); //logPath

        /*
        if (!Directory.Exists(packagePath))
        {
            throw new DirectoryNotFoundException(
                $"No se encontró el paquete '{packagePath}'.");
        }
        */

        if (!Directory.Exists(packagePath))
        {
            execution.Status = ExecutionStatus.Completed;
            execution.StartedAt = DateTime.UtcNow;
            execution.FinishedAt = DateTime.UtcNow;
            
            execution.Status = ExecutionStatus.Failed;
            execution.Error = $"No existe la carpeta del paquete '{packagePath}'. Se omite la ejecución.";

            logs.Add(new LogEntry
            {
                Name = package.Package,
                Start = execution.StartedAt.Value,
                End = execution.FinishedAt.Value,
                Ok = false,
                Msg = $"No existe la carpeta del paquete '{packagePath}'. Se omite la ejecución."
            });

            plan.Revision = plan.Revision + 1;
            int index = plan.Packages.FindIndex(x =>
            x.Package.Equals(package.Package, StringComparison.OrdinalIgnoreCase));
            plan.Packages[index].Approved = false;

            _migrationPlanService.Save(projectPath, plan);

            return;
        }


        IReadOnlyList<string> artifacts =
            ArtifactDiscovery.Discover(packagePath);

        //Si tiene ETL, retiro los demás artefactos y ejecuto solo el ETL
        if (package.SelfContainedEtl)
        {
            artifacts = artifacts
                .Where(a => a.EndsWith(".dtsx", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            if (artifacts.Any(a => a.EndsWith(".dtsx", StringComparison.OrdinalIgnoreCase)))
            {
                artifacts = artifacts
                    .Where(a =>
                    {
                        string fileName = Path.GetFileName(a);

                        return fileName.StartsWith("BEGIN_", StringComparison.OrdinalIgnoreCase) ||
                               fileName.StartsWith("DDL_", StringComparison.OrdinalIgnoreCase) ||
                               fileName.StartsWith("END_", StringComparison.OrdinalIgnoreCase) ||
                               a.EndsWith(".dtsx", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
            }
            if (!artifacts.Any(a => Path.GetFileName(a).StartsWith("SQL_", StringComparison.OrdinalIgnoreCase)) &&
                artifacts.Any(a => Path.GetFileName(a).StartsWith("LOCAL_", StringComparison.OrdinalIgnoreCase)))
            {
                List<string> ordered = [];

                ordered.AddRange(artifacts.Where(a =>
                    Path.GetFileName(a).StartsWith("BEGIN_", StringComparison.OrdinalIgnoreCase)));

                ordered.AddRange(artifacts.Where(a =>
                    Path.GetFileName(a).StartsWith("LOCAL_", StringComparison.OrdinalIgnoreCase)));

                ordered.AddRange(artifacts.Where(a =>
                    Path.GetFileName(a).StartsWith("END_", StringComparison.OrdinalIgnoreCase)));

                artifacts = ordered;
            }
        }

        //Valido si quedan artefactos para ejecutar, si no hay, lanzo excepción
        if (artifacts.Count == 0)
        {
            throw new InvalidOperationException(
                $"El paquete '{packagePath}' no contiene artefactos SQL.");
        }

        execution.Status = ExecutionStatus.Running;
        execution.StartedAt = DateTime.UtcNow;

        try
        {
            //List<LogEntry> logs = [];
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
                }catch (Exception ex)
                {
                    log.Ok = false;
                    log.Msg = ex.Message;

                    throw;
                }finally
                {
                    log.End = DateTime.UtcNow;
                    logs.Add(log);
                    //writer.EscribirLog(Path.GetFileNameWithoutExtension(artifact),[log]);
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
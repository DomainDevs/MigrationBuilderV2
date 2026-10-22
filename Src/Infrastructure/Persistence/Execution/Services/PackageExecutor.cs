using Microsoft.Extensions.Options;
using Persistence.Execution.Helpers;
using Persistence.Execution.Log;
using Persistence.Execution.Models;
using Persistence.Migration.Services;
using Shared.Options;

namespace Persistence.Execution.Services;

public sealed class PackageExecutor
{
    private readonly ArtifactExecutor _artifactExecutor;
    private readonly MigrationPlanService _migrationPlanService;
    private readonly MigrationOptions _options;

    public PackageExecutor(
        ArtifactExecutor artifactExecutor,
        MigrationPlanService migrationPlanService,
        IOptions<MigrationOptions> options)
    {
        _artifactExecutor = artifactExecutor;
        _migrationPlanService = migrationPlanService;
        _options = options.Value;
    }

    public async Task ExecuteAsyncPKG(
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

        // Cada paquete conserva únicamente sus propios logs.
        List<LogEntry> logs = [];

        DateTime startedAt = DateTime.UtcNow;

        execution.StartedAt = startedAt;
        execution.Status = ExecutionStatus.Running;

        try
        {
            if (!Directory.Exists(packagePath))
            {
                throw new DirectoryNotFoundException(
                    $"No existe la carpeta del paquete '{packagePath}'.");
            }

            IReadOnlyList<string> artifacts =
                DiscoverPackageArtifacts(packagePath, package);

            if (artifacts.Count == 0)
            {
                throw new InvalidOperationException(
                    $"El paquete '{packagePath}' no contiene artefactos ejecutables.");
            }

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
                        log.Ok = false;
                        log.Msg = "Ejecución cancelada antes de iniciar el artefacto.";
                        break;
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

                    // El resultado queda asociado al paquete para la Web.
                    execution.Logs.Add(log);

                    // El resultado también se conserva para escribir el JSON.
                    logs.Add(log);
                }

                if (execution.Status == ExecutionStatus.Cancelled)
                    break;
            }

            if (execution.Status == ExecutionStatus.Running)
            {
                execution.Status = ExecutionStatus.Completed;

                _migrationPlanService.SetApproved(
                    plan,
                    package.Package,
                    true);
            }
        }
        catch (Exception ex)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.Error = ex.Message;

            // Registrar también los errores ocurridos antes de ejecutar
            // el primer artefacto, por ejemplo, carpeta inexistente.
            if (logs.Count == 0)
            {
                DateTime now = DateTime.UtcNow;

                LogEntry log = new()
                {
                    Name = package.Package,
                    Start = startedAt,
                    End = now,
                    Ok = false,
                    Msg = ex.Message
                };

                logs.Add(log);
                execution.Logs.Add(log);
            }

            throw;
        }
        finally
        {
            execution.FinishedAt = DateTime.UtcNow;

            try
            {
                // Guardar el estado del paquete, incluso cuando falla.
                _migrationPlanService.Save(
                    projectPath,
                    plan);
            }
            finally
            {
                // Escribir el log de este paquete sin esperar a la migración.
                WritePackageLog(projectPath, package.Package, logs);
            }
        }
    }

    private static IReadOnlyList<string> DiscoverPackageArtifacts(
        string packagePath,
        MigrationPackage package)
    {
        IReadOnlyList<string> artifacts =
            ArtifactDiscovery.Discover(packagePath);

        // Los paquetes ETL autónomos ejecutan únicamente archivos DTSX.
        if (package.SelfContainedEtl)
        {
            return artifacts
                .Where(artifact =>
                    artifact.EndsWith(
                        ".dtsx",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Si existen DTSX, conservar los artefactos de preparación,
        // definición, finalización y los propios paquetes ETL.
        if (artifacts.Any(artifact =>
            artifact.EndsWith(
                ".dtsx",
                StringComparison.OrdinalIgnoreCase)))
        {
            artifacts = artifacts
                .Where(artifact =>
                {
                    string fileName = Path.GetFileName(artifact);

                    return fileName.StartsWith(
                               "BEGIN_",
                               StringComparison.OrdinalIgnoreCase)
                           || fileName.StartsWith(
                               "DDL_",
                               StringComparison.OrdinalIgnoreCase)
                           || fileName.StartsWith(
                               "END_",
                               StringComparison.OrdinalIgnoreCase)
                           || artifact.EndsWith(
                               ".dtsx",
                               StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        bool hasSql = artifacts.Any(artifact =>
            Path.GetFileName(artifact).StartsWith(
                "SQL_",
                StringComparison.OrdinalIgnoreCase));

        bool hasLocal = artifacts.Any(artifact =>
            Path.GetFileName(artifact).StartsWith(
                "LOCAL_",
                StringComparison.OrdinalIgnoreCase));

        if (!hasSql && hasLocal)
        {
            List<string> ordered = [];

            ordered.AddRange(artifacts.Where(artifact =>
                Path.GetFileName(artifact).StartsWith(
                    "BEGIN_",
                    StringComparison.OrdinalIgnoreCase)));

            ordered.AddRange(artifacts.Where(artifact =>
                Path.GetFileName(artifact).StartsWith(
                    "LOCAL_",
                    StringComparison.OrdinalIgnoreCase)));

            ordered.AddRange(artifacts.Where(artifact =>
                Path.GetFileName(artifact).StartsWith(
                    "END_",
                    StringComparison.OrdinalIgnoreCase)));

            artifacts = ordered;
        }

        return artifacts;
    }

    private void WritePackageLog(
        string projectPath,
        string packageName,
        List<LogEntry> logs)
    {
        string logDirectory = Path.Combine(
            projectPath,
            _options.Folders.Logs);

        LogWriterJSON writer = new(logDirectory);

        string safePackageName = string.Join(
            "_",
            packageName.Split(Path.GetInvalidFileNameChars()));

        writer.EscribirLog(
            safePackageName,
            logs);
    }
}

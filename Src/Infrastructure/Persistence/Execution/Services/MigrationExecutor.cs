using Application.Abstractions.Execution;
using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Execution.Helpers;
using Persistence.Execution.Log;
using Persistence.Execution.Models;
using Persistence.Health.DTOs;
using Persistence.Health.Services;
using Persistence.Migration.Services;
using Shared.Options;
using System.Numerics;

namespace Persistence.Execution.Services;

public sealed class MigrationExecutor : IMigrationExecutor
{
    private readonly MigrationPlanService _migrationPlanService;
    private readonly SqlProcessExecutor _sqlProcessExecutor;
    private readonly PackageExecutor _packageExecutor;
    private readonly DatabaseHealthService _databaseHealthService;
    private readonly ILogger<DatabaseHealthService> _logger;
    private readonly MigrationOptions _options;
    

    public MigrationExecutor(
        MigrationPlanService migrationPlanService,
        SqlProcessExecutor sqlProcessExecutor,
        PackageExecutor packageExecutor,
        DatabaseHealthService databaseHealthService,
        ILogger<DatabaseHealthService> logger,
        IOptions<MigrationOptions> options)
    {
        _migrationPlanService = migrationPlanService;
        _sqlProcessExecutor = sqlProcessExecutor;
        _packageExecutor = packageExecutor;
        _databaseHealthService = databaseHealthService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<MigrationExecuteResponse> ExecuteAsync(
        MigrationExecuteCommand command,
        CancellationToken cancellationToken)
    {
        string projectPath = command.ProjectName;

        List<LogEntry> logs = [];

        command.Packages.RemoveAll(x =>
            string.Equals(x, "string", StringComparison.OrdinalIgnoreCase));

        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        projectPath =
            Path.Combine(
                _options.Folders.Root,
                projectPath);

        if (!Directory.Exists(projectPath))
        {
            throw new IOException(
                $"El proyecto '{command.ProjectName}', no se encuentra registrado.");
        }

        // Validación previa obligatoria.
        IReadOnlyList<DatabaseHealthResult> health =
            await _databaseHealthService.CheckAsync();

        DatabaseHealthResult? failure =
            health.FirstOrDefault(x =>
                (x.Name is "Source" or "Target") &&
                x.DatabaseStatus != HealthStatus.Healthy);

        if (failure is not null)
        {
            throw new IOException(
                $"Migración abortada: la conexión '{failure.Name}' " +
                $"no está disponible. " +
                $"{failure.Error ?? "No fue posible validar la conexión a la base de datos."}");
        }

        LogWriterJSON writer =
            new($"{projectPath}\\{_options.Folders.Logs}\\");

        MigrationPlan plan =
            _migrationPlanService.Load(projectPath);

        //PreHook
        /*
        if (plan.Hooks.Pre.Enabled && plan.Hooks.Pre.Approved == false)
        {
            await _sqlProcessExecutor.ExecuteAsync(
            Path.Combine(
                projectPath,
                _options.Folders.MigrationTask.DirectoryName,
                _options.Folders.MigrationTask.PreHook),
            cancellationToken);

            plan.Hooks.Pre.Approved = true;

            _migrationPlanService.Save(projectPath, plan);
        }*/

        // PreHook
        if (plan.Hooks.Pre.Enabled && !plan.Hooks.Pre.Approved)
        {
            string preHookPath = Path.Combine(
                projectPath,
                _options.Folders.MigrationTask.DirectoryName,
                _options.Folders.MigrationTask.PreHook);

            _logger.LogInformation(
                "Iniciando PreHook. Path={Path}",
                preHookPath);

            try
            {
                await _sqlProcessExecutor.ExecuteAsync(
                    preHookPath,
                    cancellationToken);

                plan.Hooks.Pre.Approved = true;

                _migrationPlanService.Save(projectPath, plan);

                _logger.LogInformation(
                    "PreHook ejecutado correctamente y plan actualizado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error ejecutando PreHook. Path={Path}",
                    preHookPath);

                throw;
            }
        }

        MigrationExecution execution = new();

        IReadOnlyList<MigrationStage> stages =
            _migrationPlanService.GetStages(plan);

        if (command.Packages.Count > 0)
        {
            stages = stages
                .Select(stage =>
                {
                    MigrationStage filtered = new()
                    {
                        Stage = stage.Stage
                    };

                    filtered.Packages.AddRange(
                        stage.Packages.Where(x =>
                            command.Packages.Contains(
                                x.Package,
                                StringComparer.OrdinalIgnoreCase)));

                    return filtered;
                })
                .Where(stage => stage.Packages.Count > 0)
                .ToList();
        }

        foreach (MigrationStage stage in stages.OrderBy(s => s.Stage))
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Task> tasks = [];

            foreach (MigrationPackage package in stage.Packages)
            {
                if (!package.Enabled)
                    continue;

                if (package.Approved)
                    continue;

                string packagePath =
                    Path.Combine(
                        projectPath,
                        _options.Folders.MigrationTask.DirectoryName,
                        _options.Folders.MigrationTask.DataIngestion,
                        package.Package);

                PackageExecution packageExecution = new()
                {
                    Package = package.Package
                };

                execution.Packages.Add(packageExecution);

                tasks.Add(
                    _packageExecutor.ExecuteAsyncPKG(
                        projectPath,
                        packagePath,
                        plan,
                        package,
                        packageExecution,
                        logs));
            }

            await Task.WhenAll(tasks);
        }

        writer.EscribirLog(
            $"{projectPath}\\{_options.Folders.Logs}\\",
            logs);

        //PostHook
        if (plan.Hooks.Post.Enabled && plan.Hooks.Post.Approved == false)
        {
            await _sqlProcessExecutor.ExecuteAsync(
            Path.Combine(
                projectPath,
                _options.Folders.MigrationTask.DirectoryName,
                _options.Folders.MigrationTask.PostHook),
            cancellationToken);

            plan.Hooks.Post.Approved = true;

            _migrationPlanService.Save(projectPath, plan);
        }

        return new MigrationExecuteResponse
        {
            Packages = execution.Packages
                .Select(x => new PackageExecutionResponse
                {
                    Package = x.Package,
                    Status = x.Status.ToString()
                })
                .ToList()
        };
    }
}
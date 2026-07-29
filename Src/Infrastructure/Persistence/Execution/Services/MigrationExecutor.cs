using Application.Abstractions.Execution;
using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs;
using Microsoft.Extensions.Options;
using Persistence.Execution.Helpers;
using Persistence.Execution.Log;
using Persistence.Execution.Models;
using Persistence.Migration.Services;
using Shared.Options;

namespace Persistence.Execution.Services;

public sealed class MigrationExecutor : IMigrationExecutor
{
    private readonly MigrationPlanService _migrationPlanService;
    private readonly PackageExecutor _packageExecutor;
    private readonly MigrationOptions _options;

    public MigrationExecutor(
        MigrationPlanService migrationPlanService,
        PackageExecutor packageExecutor,
        IOptions<MigrationOptions> options)
    {
        _migrationPlanService = migrationPlanService;
        _packageExecutor = packageExecutor;
        _options = options.Value;
    }

    public async Task<MigrationExecuteResponse> ExecuteAsync(
        MigrationExecuteCommand command
        )
    {
        string projectPath = command.ProjectName;
        List<LogEntry> logs = [];

        command.Packages.RemoveAll(x =>
        string.Equals(x, "string", StringComparison.OrdinalIgnoreCase));

        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        //ruta fisica
        projectPath =
            Path.Combine(
                _options.Folders.Root, projectPath);

        if (!Directory.Exists(projectPath))
        {
            throw new IOException(
                $"El proyecto '{command.ProjectName}', no se encuentra registrado.");
        }

        LogWriterJSON writer = new($"{projectPath}\\{_options.Folders.Logs}\\"); 

        MigrationPlan plan =
            _migrationPlanService.Load(projectPath);

        MigrationExecution execution = new();

        IReadOnlyList<MigrationStage> stages =
            _migrationPlanService.GetStages(plan);

        //Filtrar por paquetes seleccionados si se proporcionan en el comando
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
            List<Task> tasks = [];

            foreach (MigrationPackage package in stage.Packages)
            {
                if (!package.Enabled)
                    continue;

                string packagePath =
                    Path.Combine(
                        Path.Combine(projectPath, _options.Folders.MigrationTask),
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
                        logs
                        )
                    );
                if (packageExecution.Status == ExecutionStatus.Failed)
                    package.Approved = false;
            }
            await Task.WhenAll(tasks);
        }

        writer.EscribirLog($"{projectPath}\\{_options.Folders.Logs}\\", logs);

        return new MigrationExecuteResponse
        {
            Packages = execution.Packages
                .Select(x => new PackageExecutionDto
                {
                    Package = x.Package,
                    Status = x.Status.ToString()
                })
                .ToList()
        };
    }
}
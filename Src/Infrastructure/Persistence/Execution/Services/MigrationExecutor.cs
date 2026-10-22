using Application.Abstractions.Execution;
using Application.Features.Orchestrator.Commands;
using Application.Features.Orchestrator.DTOs.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Execution.Models;
using Persistence.Health.DTOs;
using Persistence.Health.Services;
using Persistence.Migration.Services;
using Shared.Options;

namespace Persistence.Execution.Services;

public sealed class MigrationExecutor : IMigrationExecutor
{
    private readonly MigrationPlanService _migrationPlanService;
    private readonly SqlProcessExecutor _sqlProcessExecutor;
    private readonly PackageExecutor _packageExecutor;
    private readonly DatabaseHealthService _databaseHealthService;
    private readonly ILogger<MigrationExecutor> _logger;
    private readonly MigrationOptions _options;

    public MigrationExecutor(
        MigrationPlanService migrationPlanService,
        SqlProcessExecutor sqlProcessExecutor,
        PackageExecutor packageExecutor,
        DatabaseHealthService databaseHealthService,
        ILogger<MigrationExecutor> logger,
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
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        string projectName = command.ProjectName;

        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        command.Packages.RemoveAll(package =>
            string.Equals(
                package,
                "string",
                StringComparison.OrdinalIgnoreCase));

        string projectPath = Path.Combine(
            _options.Folders.Root,
            projectName);

        if (!Directory.Exists(projectPath))
        {
            throw new IOException(
                $"El proyecto '{projectName}' no se encuentra registrado.");
        }

        await ValidateDatabaseHealthAsync();

        MigrationPlan plan = _migrationPlanService.Load(projectPath);

        await ExecutePreHookAsync(
            projectPath,
            plan,
            cancellationToken);

        MigrationExecution execution = new();

        IReadOnlyList<MigrationStage> stages =
            GetStagesToExecute(plan, command.Packages);

        foreach (MigrationStage stage in stages.OrderBy(stage => stage.Stage))
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Iniciando etapa de migración {Stage}.",
                stage.Stage);

            List<Task> tasks = [];

            foreach (MigrationPackage package in stage.Packages)
            {
                if (!package.Enabled || package.Approved)
                    continue;

                string packagePath = Path.Combine(
                    projectPath,
                    _options.Folders.MigrationTask.DirectoryName,
                    _options.Folders.MigrationTask.DataIngestion,
                    package.Package);

                PackageExecution packageExecution = new()
                {
                    Package = package.Package
                };

                execution.Packages.Add(packageExecution);

                _logger.LogInformation(
                    "Iniciando paquete {Package}.",
                    package.Package);

                tasks.Add(
                    _packageExecutor.ExecuteAsyncPKG(
                        projectPath,
                        packagePath,
                        plan,
                        package,
                        packageExecution));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Etapa de migración {Stage} finalizada.",
                stage.Stage);
        }

        await ExecutePostHookAsync(
            projectPath,
            plan,
            cancellationToken);

        return new MigrationExecuteResponse
        {
            Packages = execution.Packages
                .Select(package => new PackageExecutionResponse
                {
                    Package = package.Package,
                    Status = package.Status.ToString()
                })
                .ToList()
        };
    }

    private async Task ValidateDatabaseHealthAsync()
    {
        IReadOnlyList<DatabaseHealthResult> health =
            await _databaseHealthService.CheckAsync();

        DatabaseHealthResult? failure = health.FirstOrDefault(result =>
            (result.Name is "Source" or "Target") &&
            result.DatabaseStatus != HealthStatus.Healthy);

        if (failure is not null)
        {
            throw new IOException(
                $"Migración abortada: la conexión '{failure.Name}' " +
                "no está disponible. " +
                $"{failure.Error ?? "No fue posible validar la conexión a la base de datos."}");
        }
    }

    private IReadOnlyList<MigrationStage> GetStagesToExecute(
        MigrationPlan plan,
        IReadOnlyCollection<string> selectedPackages)
    {
        IReadOnlyList<MigrationStage> stages =
            _migrationPlanService.GetStages(plan);

        if (selectedPackages.Count == 0)
            return stages;

        HashSet<string> selection = new(
            selectedPackages,
            StringComparer.OrdinalIgnoreCase);

        return stages
            .Select(stage =>
            {
                MigrationStage filteredStage = new()
                {
                    Stage = stage.Stage
                };

                filteredStage.Packages.AddRange(
                    stage.Packages.Where(package =>
                        selection.Contains(package.Package)));

                return filteredStage;
            })
            .Where(stage => stage.Packages.Count > 0)
            .ToList();
    }

    private async Task ExecutePreHookAsync(
        string projectPath,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        if (!plan.Hooks.Pre.Enabled || plan.Hooks.Pre.Approved)
            return;

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

    private async Task ExecutePostHookAsync(
        string projectPath,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        if (!plan.Hooks.Post.Enabled || plan.Hooks.Post.Approved)
            return;

        string postHookPath = Path.Combine(
            projectPath,
            _options.Folders.MigrationTask.DirectoryName,
            _options.Folders.MigrationTask.PostHook);

        _logger.LogInformation(
            "Iniciando PostHook. Path={Path}",
            postHookPath);

        try
        {
            await _sqlProcessExecutor.ExecuteAsync(
                postHookPath,
                cancellationToken);

            plan.Hooks.Post.Approved = true;

            _migrationPlanService.Save(projectPath, plan);

            _logger.LogInformation(
                "PostHook ejecutado correctamente y plan actualizado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error ejecutando PostHook. Path={Path}",
                postHookPath);

            throw;
        }
    }
}

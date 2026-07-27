using Persistence.Execution.Models;
using Persistence.Migration.Services;
using System.Text.Json;

namespace Persistence.Execution.Services;

public sealed class MigrationPlanService
{
    private const string FileName = "MigrationPlan.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public bool Exists(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        return File.Exists(GetPlanPath(projectPath));
    }

    public MigrationPlan Load(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        string file = GetPlanPath(projectPath);

        if (!File.Exists(file))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo MigrationPlan.json.",
                file);
        }

        string json = File.ReadAllText(file);

        MigrationPlan? plan =
            JsonSerializer.Deserialize<MigrationPlan>(json);

        if (plan is null)
        {
            throw new InvalidOperationException(
                "No fue posible leer MigrationPlan.json.");
        }

        return plan;
    }

    public void Save(
        string projectPath,
        MigrationPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(plan);

        string json =
            JsonSerializer.Serialize(
                plan,
                JsonOptions);

        File.WriteAllText(
            GetPlanPath(projectPath),
            json);
    }

    public IReadOnlyList<MigrationStage> GetStages(
        MigrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        List<MigrationStage> stages = [];

        foreach (MigrationPackage package in plan.Packages)
        {
            MigrationStage? stage = null;

            foreach (MigrationStage item in stages)
            {
                if (item.Stage == package.Stage)
                {
                    stage = item;
                    break;
                }
            }

            if (stage is null)
            {
                stage = new MigrationStage
                {
                    Stage = package.Stage
                };

                stages.Add(stage);
            }

            stage.Packages.Add(package);
        }

        return stages;
    }

    public IReadOnlyList<MigrationPackage> GetStage(
        MigrationPlan plan,
        int stage)
    {
        ArgumentNullException.ThrowIfNull(plan);

        List<MigrationPackage> packages = [];

        foreach (MigrationPackage package in plan.Packages)
        {
            if (package.Stage == stage)
            {
                packages.Add(package);
            }
        }

        return packages;
    }

    public MigrationPackage? GetPackage(
        MigrationPlan plan,
        string packageName)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        foreach (MigrationPackage package in plan.Packages)
        {
            if (string.Equals(
                package.Package,
                packageName,
                StringComparison.OrdinalIgnoreCase))
            {
                return package;
            }
        }

        return null;
    }

    public void SetApproved(
        MigrationPlan plan,
        string packageName,
        bool approved)
    {
        MigrationPackage package =
            GetPackage(plan, packageName)
            ?? throw new InvalidOperationException(
                $"No existe el paquete '{packageName}'.");

        package.Approved = approved;
    }

    public void SetEnabled(
        MigrationPlan plan,
        string packageName,
        bool enabled)
    {
        MigrationPackage package =
            GetPackage(plan, packageName)
            ?? throw new InvalidOperationException(
                $"No existe el paquete '{packageName}'.");

        package.Enabled = enabled;
    }

    private static string GetPlanPath(
        string projectPath)
    {
        return Path.Combine(
            projectPath,
            FileName);
    }
}
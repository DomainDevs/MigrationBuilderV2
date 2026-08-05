using Infrastructure.ProjectSystem.Provisioning;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Infrastructure.ProjectSystem;

public sealed class MigrationProjectInitializer : IMigrationProjectInitializer
{
    private readonly MigrationOptions _options;

    public MigrationProjectInitializer(
        IOptions<MigrationOptions> options)
    {
        _options = options.Value;
    }

    public void CreateProject(string ProjectName)
    {
        string JobName = _options.JobName;
        string root = _options.Folders.Root;
        string projectPath = Path.Combine(root, ProjectName);

        string jsonPlan = "{\r\n  \"Version\": \"1.0\",\r\n  \"Packages\": [\r\n  ]\r\n}";
        string txtContent = "[.ShellClassInfo]\r\nIconResource=C:\\WINDOWS\\System32\\SHELL32.dll,264\r\n[ViewState]\r\nMode=\r\nVid=\r\nFolderType=Generic\r\n";

        using ProvisioningFlow provisioning = new();

        if (Directory.Exists(projectPath))
        {
            throw new IOException($"El proyecto '{projectPath}' ya existe.");
        }
        
        provisioning
            .AddDirectory(root)
            .AddDirectory(Path.Combine(projectPath, _options.Folders.MigrationTask))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.Logs))

            .AddFile(
                //@"D:\Migration\desktop.ini",
                Path.Combine(root, "desktop.ini"),
                txtContent)

            .AddFile(
                //@"D:\Migration\MigrationPlan.json",
                Path.Combine(projectPath, "MigrationPlan.json"),
                jsonPlan)

            .SetAttributes(
                root,
                FileAttributes.System)
            .AddAction(() =>
            {
                Console.WriteLine("Migración creada.");
            });
        provisioning.Run();

    }
    public void DeleteProject(string ProjectName)
    {

    }

    public void CreateArtefac(string ProjectName, string TableName)
    {
        string JobName = _options.JobName;
        string root = _options.Folders.Root;
        string projectPath = Path.Combine(root, ProjectName);
    }
    public void DeleteArtefac(string ProjectName, string TableName)
    {

    }
}

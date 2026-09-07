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

        var task = _options.Folders.MigrationTask;

        // 1. Ruta base del contenedor: E:\Migration\MigrationTask
        provisioning
            .AddDirectory(root)
            .AddDirectory(Path.Combine(projectPath, _options.Folders.MigrationTask.DirectoryName))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.MigrationTask.DirectoryName, _options.Folders.MigrationTask.PreHook))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.MigrationTask.DirectoryName, _options.Folders.MigrationTask.DataIngestion))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.MigrationTask.DirectoryName, _options.Folders.MigrationTask.PostHook))

            .AddDirectory(Path.Combine(projectPath, _options.Folders.Logs))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.Reports))
            //@"D:\Migration\desktop.ini",
            .AddFile(
                Path.Combine(root, "desktop.ini"),
                txtContent)
            //@"D:\Migration\MigrationPlan.json",
            .AddFile(
                Path.Combine(projectPath, "MigrationPlan.json"),
                jsonPlan)
            //Directory
            .SetAttributes(
                root,
                FileAttributes.System)
            .AddAction(() =>
            {
                Console.WriteLine("Migración creada.");
            });
        provisioning.Run();

    }
    private static bool HasWritePermission(string targetPath)
    {
        if (!Directory.Exists(targetPath))
            return false;

        string tempFile = Path.Combine(targetPath, $"{Guid.NewGuid():N}.tmp");
        try
        {
            // Intenta crear y escribir un byte
            using (FileStream fs = File.Create(tempFile, 1, FileOptions.DeleteOnClose))
            {
                fs.WriteByte(0);
            }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public void DeleteProject(string ProjectName)
    {

    }

    public void CreateArtefac(string ProjectName, string TableName)
    {
        string JobName = _options.JobName;
        string root = _options.Folders.Root;
        string projectPath = Path.Combine(root, ProjectName);

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"El proyecto '{ProjectName}' no existe.");
        }

        // Ruta: E:\Migration\{ProjectName}\MigrationTask\V02_DataIngestion\{TableName}
        string artefactFolder = Path.Combine(
            projectPath,
            _options.Folders.MigrationTask.DataIngestion,
            TableName);

        if (Directory.Exists(artefactFolder))
        {
            throw new IOException($"El artefacto para '{TableName}' ya existe.");
        }
    }
    public void DeleteArtefac(string ProjectName, string TableName)
    {

    }
}

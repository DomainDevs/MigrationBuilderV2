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

    #region CreateProject
    public void CreateProject(string ProjectName)
    {
        string root = Path.GetFullPath(_options.Folders.Root);
        string projectPath = Path.Combine(root, ProjectName);

        string jsonPlan = $$"""
{
  "Version": "1.0",
  "Revision": 0,
  "GeneratedAt": "{{DateTime.UtcNow:O}}",
  "Reprocess": false,
  "Hooks": {
    "Pre": {
      "Enabled": false,
      "Approved": true
    },
    "Post": {
      "Enabled": false,
      "Approved": true
    }
  },
  "Packages": []
}
""";

        string txtContent =
            "[.ShellClassInfo]\r\n" +
            "IconResource=C:\\WINDOWS\\System32\\SHELL32.dll,264\r\n" +
            "[ViewState]\r\n" +
            "Mode=\r\n" +
            "Vid=\r\n" +
            "FolderType=Generic\r\n";

        var task = _options.Folders.MigrationTask;

        // Flujo 1: preparar la raíz si todavía no existe.
        if (!Directory.Exists(root))
        {
            using ProvisioningFlow rootFlow = new();

            rootFlow
                .AddDirectory(root)
                .SetAttributes(root, FileAttributes.System)
                .AssignReadWritePermissions(root)
                .AddFile(
                    Path.Combine(root, "desktop.ini"),
                    txtContent)
                .Run();
        }

        // Validar que el proyecto no exista.
        if (Directory.Exists(projectPath) || File.Exists(projectPath))
        {
            throw new IOException(
                $"El proyecto '{projectPath}' ya existe.");
        }

        // Flujo 2: crear la estructura del proyecto.
        string taskPath = Path.Combine(
            projectPath,
            task.DirectoryName);

        using ProvisioningFlow projectFlow = new();

        projectFlow
            .ValidateDirectory(root)
            .AddDirectory(taskPath)
            .AddDirectory(Path.Combine(taskPath, task.PreHook))
            .AddDirectory(Path.Combine(taskPath, task.DataIngestion))
            .AddDirectory(Path.Combine(taskPath, task.PostHook))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.Logs))
            .AddDirectory(Path.Combine(projectPath, _options.Folders.Reports))
            .AddFile(
                Path.Combine(projectPath, "MigrationPlan.json"),
                jsonPlan)
            .AssignReadWritePermissions(projectPath)
            .AddAction(() =>
            {
                Console.WriteLine("Migración creada.");
            })
            .Run();
    }
    #endregion

    #region CreateArtefac
    public void CreateArtefac(string ProjectName, string TableName)
    {
        ValidateName(ProjectName, nameof(ProjectName));
        ValidateName(TableName, nameof(TableName));

        string root = Path.GetFullPath(_options.Folders.Root);
        string projectPath = GetProjectPath(root, ProjectName);

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException(
                $"El proyecto '{ProjectName}' no existe.");
        }

        var task = _options.Folders.MigrationTask;

        string artefactFolder = Path.Combine(
            projectPath,
            task.DirectoryName,
            task.DataIngestion,
            TableName);

        if (Directory.Exists(artefactFolder) || File.Exists(artefactFolder))
        {
            throw new IOException(
                $"El artefacto para '{TableName}' ya existe.");
        }

        using ProvisioningFlow flow = new();

        flow
            .ValidateDirectory(projectPath)
            .AddDirectory(artefactFolder)
            .AssignReadWritePermissions(artefactFolder)
            .Run();
    }
    #endregion

    #region DeleteProject
    public void DeleteProject(string name)
    {
        ValidateName(name, nameof(name));

        string root = Path.GetFullPath(_options.Folders.Root);
        string projectPath = GetProjectPath(root, name);

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException(
                $"El proyecto '{name}' no existe.");
        }

        using ProvisioningFlow flow = new();

        flow
            .ValidateDirectory(root)
            .DeleteDirectory(projectPath, recursive: true)
            .Run();
    }
    #endregion

    #region DeleteArtefac
    public void DeleteArtefac(string ProjectName, string TableName)
    {
        ValidateName(ProjectName, nameof(ProjectName));
        ValidateName(TableName, nameof(TableName));

        string root = Path.GetFullPath(_options.Folders.Root);
        string projectPath = GetProjectPath(root, ProjectName);

        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException(
                $"El proyecto '{ProjectName}' no existe.");
        }

        var task = _options.Folders.MigrationTask;

        string artefactFolder = Path.Combine(
            projectPath,
            task.DirectoryName,
            task.DataIngestion,
            TableName);

        if (!Directory.Exists(artefactFolder))
        {
            throw new DirectoryNotFoundException(
                $"El artefacto para '{TableName}' no existe.");
        }

        using ProvisioningFlow flow = new();

        flow
            .ValidateDirectory(projectPath)
            .DeleteDirectory(artefactFolder, recursive: true)
            .Run();
    }
    #endregion

    #region ValidateName
    private static void ValidateName(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value is "." or ".." ||
            Path.IsPathRooted(value) ||
            value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            value.Contains('/') ||
            value.Contains('\\'))
        {
            throw new ArgumentException(
                "El valor debe ser un nombre válido, no una ruta.",
                parameterName);
        }
    }
    #endregion

    #region GetProjectPath
    private static string GetProjectPath(string root, string name)
    {
        string projectPath = Path.GetFullPath(
            Path.Combine(root, name));

        string relativePath = Path.GetRelativePath(root, projectPath);

        if (relativePath == "." ||
            relativePath == ".." ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                "La ruta del proyecto debe permanecer dentro de la raíz configurada.");
        }

        return projectPath;
    }
    #endregion

}

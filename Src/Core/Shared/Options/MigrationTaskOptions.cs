namespace Shared.Options;

public sealed class MigrationTaskOptions
{
    public string DirectoryName { get; init; } = string.Empty;

    public string PreHook { get; init; } = string.Empty;

    public string DataIngestion { get; init; } = string.Empty;

    public string PostHook { get; init; } = string.Empty;

    // Métodos helper para resolver las rutas absolutas dentro del pipeline
    public string GetPreHookFullPath(string root) =>
        Path.Combine(root, DirectoryName, PreHook);

    public string GetDataIngestionFullPath(string root) =>
        Path.Combine(root, DirectoryName, DataIngestion);

    public string GetPostHookFullPath(string root) =>
        Path.Combine(root, DirectoryName, PostHook);
}

namespace Shared.Options;

public sealed class FolderOptions
{
    public string Root { get; init; } = string.Empty;

    public string MigrationTask { get; init; } = string.Empty; //Artifacts

    public string Logs { get; init; } = string.Empty;

    public string Reports { get; init; } = string.Empty;
}
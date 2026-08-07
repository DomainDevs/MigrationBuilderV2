using System;

namespace Shared.Options;

public sealed class MigrationOptions
{
    public const string SectionName = "Migration";

    public string JobName { get; init; } = string.Empty;

    public FolderOptions Folders { get; init; } = new();

    public ExecutionOptions Execution { get; init; } = new();
}

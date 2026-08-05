namespace Infrastructure.ProjectSystem.Provisioning;

/// <summary>
/// Defines a provisioning flow that builds and executes
/// an ordered sequence of file system operations.
///
/// Once executed, the flow cannot be reused.
/// </summary>
public sealed class ProvisioningFlow : IDisposable
{
    /// <summary>
    /// Ordered list of operations that compose the flow.
    /// </summary>
    private readonly List<ProvisioningOperation> _operations = [];

    /// <summary>
    /// Indicates whether the flow has already been executed.
    /// Prevents the flow from being reused.
    /// </summary>
    private bool _executed;

    public ProvisioningFlow AddDirectory(string path)
    {
        EnsureNotExecuted();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _operations.Add(
            new ProvisioningOperation
            {
                Type = ProvisioningOperationType.CreateDirectory,
                Path = path
            });

        return this;
    }

    public ProvisioningFlow AddFile(
        string path,
        string content = "")
    {
        EnsureNotExecuted();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _operations.Add(
            new ProvisioningOperation
            {
                Type = ProvisioningOperationType.CreateFile,
                Path = path,
                Content = content
            });

        return this;
    }

    public ProvisioningFlow SetAttributes(
        string path,
        FileAttributes attributes)
    {
        EnsureNotExecuted();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _operations.Add(
            new ProvisioningOperation
            {
                Type = ProvisioningOperationType.SetAttributes,
                Path = path,
                Attributes = attributes
            });

        return this;
    }

    public ProvisioningFlow ValidateDrive(string path)
    {
        EnsureNotExecuted();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _operations.Add(
            new ProvisioningOperation
            {
                Type = ProvisioningOperationType.ValidateDrive,
                Path = path
            });

        return this;
    }

    public ProvisioningFlow ValidateDirectory(string path)
    {
        EnsureNotExecuted();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _operations.Add(
            new ProvisioningOperation
            {
                Type = ProvisioningOperationType.ValidateDirectory,
                Path = path
            });

        return this;
    }

    public ProvisioningFlow AddAction(Action action)
    {
        EnsureNotExecuted();
        ArgumentNullException.ThrowIfNull(action);

        _operations.Add(
            new ProvisioningOperation
            {
                Type = ProvisioningOperationType.Action,
                Callback = action
            });

        return this;
    }

    /// <summary>
    /// Executes all queued operations sequentially.
    /// The flow is automatically released after execution.
    /// </summary>
    public void Run()
    {
        EnsureNotExecuted();

        _executed = true;

        try
        {
            foreach (ProvisioningOperation operation in _operations)
            {
                switch (operation.Type)
                {
                    case ProvisioningOperationType.ValidateDrive:
                    case ProvisioningOperationType.ValidateDirectory:

                        if (!Directory.Exists(operation.Path!))
                        {
                            throw new DirectoryNotFoundException(operation.Path);
                        }

                        break;

                    case ProvisioningOperationType.CreateDirectory:

                        Directory.CreateDirectory(operation.Path!);

                        break;

                    case ProvisioningOperationType.CreateFile:

                        string? directory =
                            Path.GetDirectoryName(operation.Path!);

                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.WriteAllText(
                            operation.Path!,
                            operation.Content ?? string.Empty);

                        break;

                    case ProvisioningOperationType.SetAttributes:

                        if (Directory.Exists(operation.Path!))
                        {
                            DirectoryInfo directoryInfo =
                                new(operation.Path!);

                            directoryInfo.Attributes |= operation.Attributes;
                        }
                        else if (File.Exists(operation.Path!))
                        {
                            File.SetAttributes(
                                operation.Path!,
                                File.GetAttributes(operation.Path!) |
                                operation.Attributes);
                        }

                        break;

                    case ProvisioningOperationType.Action:

                        operation.Callback!.Invoke();

                        break;
                }
            }
        }
        finally
        {
            // Release all queued operations immediately,
            // regardless of whether execution succeeded.
            _operations.Clear();
        }
    }

    /// <summary>
    /// Releases the resources associated with the flow.
    /// </summary>
    public void Dispose()
    {
        _operations.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Ensures the flow has not already been executed.
    /// </summary>
    private void EnsureNotExecuted()
    {
        if (_executed)
        {
            throw new InvalidOperationException(
                "ProvisioningEngine has already been executed.");
        }
    }
}
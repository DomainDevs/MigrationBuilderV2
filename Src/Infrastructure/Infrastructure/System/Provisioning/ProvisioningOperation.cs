
namespace Infrastructure.System.Provisioning;

internal sealed class ProvisioningOperation
{
    public ProvisioningOperationType Type { get; set; }
    public string? Path { get; set; }
    public string? Content { get; set; }
    public FileAttributes Attributes { get; set; }
    public Action? Callback { get; set; }
}


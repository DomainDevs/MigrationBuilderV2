namespace Infrastructure.System.Provisioning;

public enum ProvisioningOperationType
{
    ValidateDrive,
    ValidateDirectory,
    CreateDirectory,
    CreateFile,
    SetAttributes,
    Action
}
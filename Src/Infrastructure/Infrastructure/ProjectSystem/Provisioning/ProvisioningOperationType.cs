namespace Infrastructure.ProjectSystem.Provisioning;

public enum ProvisioningOperationType
{
    ValidateDrive,
    ValidateDirectory,
    CreateDirectory,
    CreateFile,
    SetAttributes,
    ApplyReadWritePermissions,
    DeleteFile,
    DeleteDirectory,
    Action
}
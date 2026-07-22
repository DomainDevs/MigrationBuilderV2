namespace Domain.Enums;

public enum MigrationWarning
{
    None = 0,

    DataTypeMismatch,

    LengthMismatch,

    PrecisionMismatch,

    ScaleMismatch,

    NullableMismatch,

    MissingTargetColumn,

    MissingSourceColumn,

    IdentityColumn
}
using DataToolkit.Library;
using Domain.Enums;
using Persistence.Migration.Helpers;

namespace Persistence.Migration.Builders;

internal static class Common
{
    #region BuildSqlType
    public static string BuildSqlType(ColumnMetadata column)
    {
        var type = column.SqlType.ToLowerInvariant();

        return type switch
        {
            "char" or "varchar" or "nchar" or "nvarchar"
            or "binary" or "varbinary"
                => string.IsNullOrWhiteSpace(column.MaxLength)
                    ? column.SqlType
                    : $"{column.SqlType}({column.MaxLength})",

            "decimal" or "numeric"
                => string.IsNullOrWhiteSpace(column.Precision) ||
                   string.IsNullOrWhiteSpace(column.Scale)
                    ? column.SqlType
                    : $"{column.SqlType}({column.Precision},{column.Scale})",

            "datetime2" or "datetimeoffset" or "time"
                => string.IsNullOrWhiteSpace(column.Scale)
                    ? column.SqlType
                    : $"{column.SqlType}({column.Scale})",

            _ => column.SqlType
        };
    }
    #endregion

    #region BuildWarning
    public static string BuildWarning(
        ColumnMetadata? source,
        ColumnMetadata target)
    {
        if (source is null)
            return string.Empty;

        string sourceType = BuildSqlType(source);
        string targetType = BuildSqlType(target);

        // Tipo de dato
        if (!source.SqlType.Equals(
                target.SqlType,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. {MigrationWarning.DataTypeMismatch.GetMessage()} */";
        }

        // Longitud
        if (int.TryParse(source.MaxLength, out var sourceLength) &&
            int.TryParse(target.MaxLength, out var targetLength) &&
            sourceLength > targetLength)
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. {MigrationWarning.LengthMismatch.GetMessage()} */";
        }

        // Precisión
        if (int.TryParse(source.Precision, out var sourcePrecision) &&
            int.TryParse(target.Precision, out var targetPrecision) &&
            sourcePrecision > targetPrecision)
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. {MigrationWarning.PrecisionMismatch.GetMessage()} */";
        }

        // Escala
        if (int.TryParse(source.Scale, out var sourceScale) &&
            int.TryParse(target.Scale, out var targetScale) &&
            sourceScale > targetScale)
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. {MigrationWarning.ScaleMismatch.GetMessage()} */";
        }

        // Nullable
        if (source.IsNullable && !target.IsNullable)
        {
            return
                $" /* WARNING: Source NULL -> Target NOT NULL. {MigrationWarning.NullableMismatch.GetMessage()} */";
        }

        // Identity
        if (target.IsIdentity)
        {
            return
                $" /* WARNING: Columna : IDENTITY. {MigrationWarning.IdentityColumn.GetMessage()} */";
        }

        return string.Empty;
    }
    #endregion


}

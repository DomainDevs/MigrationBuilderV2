using DataToolkit.Library;
using Domain.Enums;
using Persistence.Migration.Helpers;

namespace Persistence.Migration.Builders;

internal static class Common
{
    #region BuildSqlType
    public static string BuildSqlType(ColumnMetadata column)
    {
        string sqlType =
            string.IsNullOrWhiteSpace(column.BaseSqlType)
                ? column.SqlType
                : column.BaseSqlType;

        string type =
            sqlType.ToLowerInvariant();

        return type switch
        {
            "char" or "varchar" or "nchar" or "nvarchar"
            or "binary" or "varbinary"
                => string.IsNullOrWhiteSpace(column.MaxLength)
                    ? sqlType
                    : $"{sqlType}({column.MaxLength})",

            "decimal" or "numeric"
                => string.IsNullOrWhiteSpace(column.Precision) ||
                   string.IsNullOrWhiteSpace(column.Scale)
                    ? sqlType
                    : $"{sqlType}({column.Precision},{column.Scale})",

            "datetime2" or "datetimeoffset" or "time"
                => string.IsNullOrWhiteSpace(column.Scale)
                    ? sqlType
                    : $"{sqlType}({column.Scale})",

            _ => sqlType
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

        string sourceType =
            BuildSqlType(source);

        string targetType =
            BuildSqlType(target);

        string sourceBaseType =
            string.IsNullOrWhiteSpace(source.BaseSqlType)
                ? source.SqlType
                : source.BaseSqlType;

        string targetBaseType =
            string.IsNullOrWhiteSpace(target.BaseSqlType)
                ? target.SqlType
                : target.BaseSqlType;

        // Tipo de dato
        if (!sourceBaseType.Equals(
                targetBaseType,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. " +
                $"{MigrationWarning.DataTypeMismatch.GetMessage()} */";
        }

        // Longitud
        if (int.TryParse(source.MaxLength, out int sourceLength) &&
            int.TryParse(target.MaxLength, out int targetLength) &&
            sourceLength > targetLength)
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. " +
                $"{MigrationWarning.LengthMismatch.GetMessage()} */";
        }

        // Precisión
        if (int.TryParse(source.Precision, out int sourcePrecision) &&
            int.TryParse(target.Precision, out int targetPrecision) &&
            sourcePrecision > targetPrecision)
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. " +
                $"{MigrationWarning.PrecisionMismatch.GetMessage()} */";
        }

        // Escala
        if (int.TryParse(source.Scale, out int sourceScale) &&
            int.TryParse(target.Scale, out int targetScale) &&
            sourceScale > targetScale)
        {
            return
                $" /* WARNING: Source {sourceType} -> Target {targetType}. " +
                $"{MigrationWarning.ScaleMismatch.GetMessage()} */";
        }

        // Nullable
        if (source.IsNullable && !target.IsNullable)
        {
            return
                $" /* WARNING: Source NULL -> Target NOT NULL. " +
                $"{MigrationWarning.NullableMismatch.GetMessage()} */";
        }

        // Identity
        if (target.IsIdentity)
        {
            return
                $" /* WARNING: Columna : IDENTITY. " +
                $"{MigrationWarning.IdentityColumn.GetMessage()} */";
        }

        return string.Empty;
    }
    #endregion


}

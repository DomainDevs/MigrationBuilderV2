using DataToolkit.Library;
using Domain.Enums;

namespace Persistence.Migration.Helpers;

internal static class MigrationWarningExtensions
{
    public static string GetArtifactPrefix(ArtifactType artifactType)
    {
        return artifactType switch
        {
            ArtifactType.WorkFile => "WF",
            ArtifactType.Staging => "STG",
            ArtifactType.Transformation => "HM",
            ArtifactType.Integration => "INT",
            _ => throw new ArgumentOutOfRangeException(
                nameof(artifactType),
                artifactType,
                "Tipo de artifact no soportado.")
        };
    }

    public static string GetMessage(
        this MigrationWarning warning)
    {
        return warning switch
        {
            MigrationWarning.DataTypeMismatch =>
                "Verifique tipo de dato, columna origen vs la columna destino.",

            MigrationWarning.LengthMismatch =>
                "Verifique que la longitud, columna origen vs destino.",

            MigrationWarning.PrecisionMismatch =>
                "Verifique que la precisión, columna origen vs destino.",

            MigrationWarning.ScaleMismatch =>
                "Verifique que la escala, columna origen compatible vs destino.",

            MigrationWarning.NullableMismatch =>
                "Verifique valores nulos, columnas origen vs destino.",

            MigrationWarning.MissingTargetColumn =>
                "La columna no existe en la tabla destino.",

            MigrationWarning.MissingSourceColumn =>
                "La columna no existe en la tabla origen.",

            MigrationWarning.IdentityColumn =>
                "La columna destino es IDENTITY. Verifique si debe excluirse o habilitar IDENTITY_INSERT durante la carga.",

            _ => string.Empty
        };
    }
    public static string GetDefaultValue(ColumnMetadata column)
    {
        // Si acepta NULL, no hay problema.
        //if (column.IsNullable)
        //    return "NULL";

        string dataType = column.SqlType.ToLowerInvariant();

        // Tipos texto
        if (dataType is "char"
            or "varchar"
            or "nchar"
            or "nvarchar"
            or "text"
            or "ntext")
        {
            return "''";
        }

        // Tipos numéricos
        if (dataType is "tinyint"
            or "smallint"
            or "int"
            or "bigint"
            or "decimal"
            or "numeric"
            or "float"
            or "real"
            or "money"
            or "smallmoney")
        {
            return "0";
        }

        // Bits (Sí/No)
        if (dataType == "bit")
        {
            return "0";
        }

        // Fechas
        if (dataType is "date"
            or "datetime"
            or "datetime2"
            or "smalldatetime")
        {
            return "'1900-01-01'";
        }

        // GUID
        if (dataType == "uniqueidentifier")
        {
            return "'00000000-0000-0000-0000-000000000000'";
        }

        // Valor por defecto para tipos desconocidos
        return "NULL";
    }


}
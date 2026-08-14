using System.Text;

namespace Persistence.Migration.Builders;

internal static class BeginEndBuilder
{
    #region BuildBegin
    public static void BuildBegin(
        string artifactFolder,
        string artifactPrefix,
        string schema,
        string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        string packagePath =
            Path.Combine(
                artifactFolder,
                $"{schema}.{tableName}");

        Directory.CreateDirectory(packagePath);

        string filePath =
            Path.Combine(
                packagePath,
                $"BEGIN_{schema}.{artifactPrefix}_{tableName}.sql");

        var sql = new StringBuilder();

        sql.AppendLine("/*");
        sql.AppendLine($"    Package  : {schema}.{tableName}");
        sql.AppendLine("    Artifact : BEGIN");
        sql.AppendLine("*/");
        sql.AppendLine();

        sql.AppendLine("-- ===========================================================");
        sql.AppendLine("-- Preparación de la migración");
        sql.AppendLine("-- Descomente únicamente las instrucciones necesarias.");
        sql.AppendLine("-- ===========================================================");
        sql.AppendLine();

        sql.AppendLine("-- Deshabilitar restricciones");
        sql.AppendLine(
            $"-- ALTER TABLE [{schema}].[{tableName}] NOCHECK CONSTRAINT ALL;");
        sql.AppendLine();

        sql.AppendLine("-- Deshabilitar triggers");
        sql.AppendLine(
            $"-- DISABLE TRIGGER ALL ON [{schema}].[{tableName}];");
        sql.AppendLine();

        int deleteBatchSize =
            artifactPrefix.Equals(
                "WF",
                StringComparison.OrdinalIgnoreCase)
                ? 6000
                : 4000;

        sql.AppendLine("-- Limpiar tabla de destino");
        sql.AppendLine("SET NOCOUNT ON;");
        sql.AppendLine("WHILE 1 = 1");
        sql.AppendLine("BEGIN");
        sql.AppendLine($"    DELETE TOP ({deleteBatchSize})");
        sql.AppendLine($"    FROM [{schema}].[{tableName}];");
        sql.AppendLine();
        sql.AppendLine("    IF @@ROWCOUNT = 0");
        sql.AppendLine("        BREAK;");
        sql.AppendLine("END;");
        sql.AppendLine();

        sql.AppendLine("-- Permitir insertar valores Identity");
        sql.AppendLine(
            $"-- SET IDENTITY_INSERT [{schema}].[{tableName}] ON;");

        File.WriteAllText(
            filePath,
            sql.ToString(),
            Encoding.UTF8);
    }
    #endregion

    #region BuildEnd
    public static void BuildEnd(
        string artifactFolder,
        string artifactPrefix,
        string schema,
        string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        string packagePath =
            Path.Combine(
                artifactFolder,
                $"{schema}.{tableName}");

        Directory.CreateDirectory(packagePath);

        string filePath =
            Path.Combine(
                packagePath,
                $"END_{schema}.{artifactPrefix}_{tableName}.sql");

        var sql = new StringBuilder();

        sql.AppendLine("/*");
        sql.AppendLine($"    Package  : {schema}.{tableName}");
        sql.AppendLine("    Artifact : END");
        sql.AppendLine("*/");
        sql.AppendLine();

        sql.AppendLine("-- ===========================================================");
        sql.AppendLine("-- Finalización de la migración");
        sql.AppendLine("-- Descomente únicamente las instrucciones necesarias.");
        sql.AppendLine("-- ===========================================================");
        sql.AppendLine();

        // La STG es temporal y puede ocupar mucho espacio.
        // Se elimina inmediatamente después de completar el LOAD.
        sql.AppendLine("-- Eliminar tabla de trabajo");
        sql.AppendLine(
            $"IF OBJECT_ID(N'[{schema}].[{artifactPrefix}_{tableName}]', N'U') IS NOT NULL");
        sql.AppendLine("BEGIN");
        sql.AppendLine(
            $"    DROP TABLE [{schema}].[{artifactPrefix}_{tableName}];");
        sql.AppendLine("END");
        sql.AppendLine();

        sql.AppendLine("-- Deshabilitar Identity Insert");
        sql.AppendLine(
            $"-- SET IDENTITY_INSERT [{schema}].[{tableName}] OFF;");
        sql.AppendLine();

        sql.AppendLine("-- Habilitar triggers");
        sql.AppendLine(
            $"-- ENABLE TRIGGER ALL ON [{schema}].[{tableName}];");
        sql.AppendLine();

        sql.AppendLine("-- Habilitar restricciones");
        sql.AppendLine(
            $"-- ALTER TABLE [{schema}].[{tableName}] WITH CHECK CHECK CONSTRAINT ALL;");
        sql.AppendLine();

        sql.AppendLine("-- Reconstruir índices");
        sql.AppendLine(
            $"ALTER INDEX ALL ON [{schema}].[{tableName}] REBUILD WITH " +
            "(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, " +
            "SORT_IN_TEMPDB = OFF, ONLINE = OFF, " +
            "ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON);");
        sql.AppendLine();

        sql.AppendLine("-- Actualizar estadísticas");
        sql.AppendLine(
            $"UPDATE STATISTICS [{schema}].[{tableName}];");
        sql.AppendLine();

        sql.AppendLine("-- Verificar integridad de la tabla");
        sql.AppendLine(
            $"DBCC CHECKTABLE ('[{schema}].[{tableName}]');");
        sql.AppendLine();

        File.WriteAllText(
            filePath,
            sql.ToString(),
            Encoding.UTF8);
    }
    #endregion
}
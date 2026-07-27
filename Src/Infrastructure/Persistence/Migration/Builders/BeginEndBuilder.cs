using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        string packagePath = Path.Combine(artifactFolder, $"{schema}.{tableName}");

        Directory.CreateDirectory(packagePath);

        string filePath = Path.Combine(
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
        sql.AppendLine($"-- ALTER TABLE [{schema}].[{tableName}] NOCHECK CONSTRAINT ALL;");
        sql.AppendLine();

        sql.AppendLine("-- Deshabilitar triggers");
        sql.AppendLine($"-- DISABLE TRIGGER ALL ON [{schema}].[{tableName}];");
        sql.AppendLine();

        if (artifactPrefix == "WF")
        {        
            sql.AppendLine("-- Limpiar tabla de destino");
            sql.AppendLine($"WHILE 1 = 1");
            sql.AppendLine($"BEGIN ");
            sql.AppendLine($"   DELETE TOP(10000) ");
            sql.AppendLine($"   FROM [{schema}].[{tableName}]; ");
            sql.AppendLine($"   IF @@ROWCOUNT = 0 ");
            sql.AppendLine($"   BREAK; ");
            sql.AppendLine($"END; ");
            sql.AppendLine();
        }else
        {
            sql.AppendLine("-- Limpiar tabla de destino");
            sql.AppendLine($"TRUNCATE TABLE [{schema}].[{tableName}];");
            sql.AppendLine();
        }

        sql.AppendLine("-- Permitir insertar valores Identity");
        sql.AppendLine($"-- SET IDENTITY_INSERT [{schema}].[{tableName}] ON;");

        File.WriteAllText(filePath, sql.ToString(), Encoding.UTF8);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        string packagePath = Path.Combine(artifactFolder, $"{schema}.{tableName}");

        Directory.CreateDirectory(packagePath);

        string filePath = Path.Combine(
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

        sql.AppendLine("-- Deshabilitar Identity Insert");
        sql.AppendLine($"-- SET IDENTITY_INSERT [{schema}].[{tableName}] OFF;");
        sql.AppendLine();

        sql.AppendLine("-- Habilitar triggers");
        sql.AppendLine($"-- ENABLE TRIGGER ALL ON [{schema}].[{tableName}];");
        sql.AppendLine();

        sql.AppendLine("-- Habilitar restricciones");
        sql.AppendLine($"-- ALTER TABLE [{schema}].[{tableName}] WITH CHECK CHECK CONSTRAINT ALL;");
        sql.AppendLine();

        if (artifactPrefix == "WF")
        {
            sql.AppendLine("-- Eliminar tabla de trabajo");
            sql.AppendLine($"DROP TABLE [{schema}].[{artifactPrefix}_{tableName}];");
            sql.AppendLine();
        }

        sql.AppendLine("-- Actualizar estadísticas");
        sql.AppendLine($"-- UPDATE STATISTICS [{schema}].[{tableName}];");

        File.WriteAllText(filePath, sql.ToString(), Encoding.UTF8);
    }
    #endregion
}
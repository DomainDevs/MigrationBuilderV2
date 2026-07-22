using Serilog;
using System.Diagnostics;
using System.Text;

namespace Infrastructure.Common.Diagnostics;

public static class StartupDiagnostics
{
    public static void LogStartupError(Exception ex)
    {
        var rootCause = GetMostInnerException(ex);

        // -----------------------------------------------------------------
        // SERILOG (Log completo)
        // -----------------------------------------------------------------
        Log.Fatal(ex, "Critical startup error.");

        // -----------------------------------------------------------------
        // CONSOLA (Diagnóstico amigable)
        // -----------------------------------------------------------------
        Console.OutputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine();
        Console.WriteLine(new string('═', 70));
        Console.WriteLine("  ERROR CRÍTICO DURANTE EL INICIO DE LA APLICACIÓN");
        Console.WriteLine(new string('═', 70));

        if (EsErrorInyeccion(ex))
        {
            TipificarErrorInyeccion(ex);
        }
        else if (EsErrorBaseDeDatos(ex, rootCause))
        {
            TipificarErrorBaseDeDatos(rootCause);
        }
        else if (ex.Message.Contains("A circular dependency was detected", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(" [!] TIPO     : DEPENDENCIA CIRCULAR");
            Console.WriteLine(" [i] DETALLE  : Dos o más servicios se inyectan entre sí.");
        }
        else
        {
            Console.WriteLine(" [!] TIPO     : ERROR GENERAL DE ARRANQUE");
            Console.WriteLine($" [>] MENSAJE  : {ex.Message}");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine(" CAUSA RAÍZ");
        Console.WriteLine($" {rootCause.Message}");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.WriteLine(" El detalle completo y el StackTrace fueron registrados en Serilog.");

        Console.ResetColor();
        Console.WriteLine(new string('═', 70));

        ManejarPausaSegunEntorno();
    }

    private static void TipificarErrorBaseDeDatos(Exception root)
    {
        Console.WriteLine(" [!] TIPO     : ERROR DE BASE DE DATOS");

        string sugerencia = "Verifica la cadena de conexión.";

        if (root.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
            root.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase))
        {
            sugerencia = "El servidor de base de datos no responde.";
        }
        else if (root.Message.Contains("login failed", StringComparison.OrdinalIgnoreCase))
        {
            sugerencia = "Usuario o contraseña incorrectos.";
        }
        else if (root.Message.Contains("relation", StringComparison.OrdinalIgnoreCase) ||
                 root.Message.Contains("table", StringComparison.OrdinalIgnoreCase) ||
                 root.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            sugerencia = "La base de datos parece no estar inicializada.";
        }

        Console.WriteLine($" [x] ANÁLISIS : {sugerencia}");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(" PASOS SUGERIDOS");
        Console.WriteLine("   1. Revisar la cadena de conexión.");
        Console.WriteLine("   2. Verificar que el motor de BD esté disponible.");
        Console.WriteLine("   3. Confirmar que la base de datos exista.");
    }

    private static void TipificarErrorInyeccion(Exception ex)
    {
        string fullMessage = GetMostInnerException(ex).Message;

        var parts = fullMessage.Split('\'');

        string missingType = parts.Length > 1
            ? CleanTypeName(parts[1])
            : "No identificado";

        string consumer = parts.Length > 3
            ? CleanTypeName(parts[3])
            : "Constructor";

        Console.WriteLine(" [!] TIPO     : ERROR DE INYECCIÓN DE DEPENDENCIAS");
        Console.WriteLine($" [x] FALTANTE : {missingType}");
        Console.WriteLine($" [x] SOLICITA : {consumer}");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine(" PASOS SUGERIDOS");
        Console.WriteLine("   1. Revisar el registro del servicio.");
        Console.WriteLine("   2. Verificar AddInfrastructure().");
        Console.WriteLine("   3. Verificar AddPersistence().");
        Console.WriteLine("   4. Confirmar que Bootstrap registró la clase.");
    }

    private static bool EsErrorInyeccion(Exception ex) =>
        ex.Message.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase) ||
        (ex.InnerException?.Message.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool EsErrorBaseDeDatos(Exception ex, Exception root) =>
        ex.StackTrace?.Contains("Persistence") == true ||
        ex.StackTrace?.Contains("EntityFrameworkCore") == true ||
        root.Message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
        root.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);

    private static Exception GetMostInnerException(Exception ex)
    {
        while (ex.InnerException != null)
            ex = ex.InnerException;

        return ex;
    }

    private static string CleanTypeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return fullName;

        int index = fullName.LastIndexOf('.');

        return index >= 0
            ? fullName[(index + 1)..]
            : fullName;
    }

    [Conditional("DEBUG")]
    private static void ManejarPausaSegunEntorno()
    {
        if (!Environment.UserInteractive)
            return;

        Console.WriteLine();
        Console.Write("Presione una tecla para finalizar...");
        Console.ReadKey(intercept: true);
    }
}
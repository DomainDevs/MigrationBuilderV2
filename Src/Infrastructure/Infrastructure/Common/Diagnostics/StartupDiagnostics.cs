using Serilog;
using System.Diagnostics;
using System.Text;

namespace Infrastructure.Common.Diagnostics;

public static class StartupDiagnostics
{
    
    private const string Separator = "───────────────────────────────────────────────────────────────";

    public static void LogStartupError(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Exception root = GetRootCause(ex);

        Log.Fatal(ex, "Critical startup error.");

        Console.OutputEncoding = Encoding.UTF8;

        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine();
        Console.WriteLine(Separator);
        Console.WriteLine("  ERROR CRÍTICO DURANTE EL INICIO DE LA APLICACIÓN");
        Console.WriteLine(Separator);

        if (EsErrorInyeccion(root))
        {
            MostrarErrorInyeccion(root);
        }
        else if (EsErrorBaseDeDatos(root))
        {
            MostrarErrorBaseDatos(root);
        }
        else if (EsDependenciaCircular(root))
        {
            Console.WriteLine(" [!] TIPO     : DEPENDENCIA CIRCULAR");
            Console.WriteLine(" [i] DETALLE  : Dos o más servicios se inyectan entre sí.");
        }
        else
        {
            Console.WriteLine(" [!] TIPO     : ERROR GENERAL DE ARRANQUE");
            Console.WriteLine($" [>] EXCEPCIÓN: {root.GetType().Name}");
            Console.WriteLine($" [>] MENSAJE  : {root.Message}");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine();
        Console.WriteLine(" CAUSA RAÍZ");
        Console.WriteLine($" {root.Message}");

        Console.ForegroundColor = ConsoleColor.DarkGray;

        Console.WriteLine();
        Console.WriteLine(" El detalle completo y el StackTrace fueron registrados en Serilog.");

        Console.ForegroundColor = previousColor;

        Console.WriteLine(Separator);

        ManejarPausaSegunEntorno();
    }

    private static void MostrarErrorInyeccion(Exception root)
    {
        string missingType = "No identificado";
        string consumer = "Constructor";

        string[] parts = root.Message.Split('\'');

        if (parts.Length >= 4)
        {
            missingType = CleanTypeName(parts[1]);
            consumer = CleanTypeName(parts[3]);
        }

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

    private static void MostrarErrorBaseDatos(Exception root)
    {
        Console.WriteLine(" [!] TIPO     : ERROR DE BASE DE DATOS");

        string sugerencia = "Verifica la cadena de conexión.";

        string message = root.Message;

        if (message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("server was not found", StringComparison.OrdinalIgnoreCase))
        {
            sugerencia = "El servidor de base de datos no responde.";
        }
        else if (message.Contains("login failed", StringComparison.OrdinalIgnoreCase))
        {
            sugerencia = "Usuario o contraseña incorrectos.";
        }
        else if (message.Contains("relation", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("table", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
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

    private static bool EsErrorInyeccion(Exception ex) =>
        ex.Message.Contains(
            "Unable to resolve service",
            StringComparison.OrdinalIgnoreCase);

    private static bool EsDependenciaCircular(Exception ex) =>
        ex.Message.Contains(
            "A circular dependency was detected",
            StringComparison.OrdinalIgnoreCase);

    private static bool EsErrorBaseDeDatos(Exception ex)
    {
        string message = ex.Message;

        return message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("login failed", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("server was not found", StringComparison.OrdinalIgnoreCase);
    }

    private static Exception GetRootCause(Exception ex)
    {
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
        }

        return ex;
    }

    private static string CleanTypeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        int index = fullName.LastIndexOf('.');

        return index >= 0
            ? fullName[(index + 1)..]
            : fullName;
    }

    [Conditional("DEBUG")]
    private static void ManejarPausaSegunEntorno()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        Console.WriteLine();
        Console.Write("Presione una tecla para finalizar...");
        Console.ReadKey(intercept: true);
    }
}
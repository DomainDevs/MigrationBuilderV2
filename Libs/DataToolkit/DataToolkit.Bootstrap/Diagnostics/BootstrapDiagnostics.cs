using System.Diagnostics;
using System.Text;

namespace DataToolkit.Bootstrap.Diagnostics;

public static class BootstrapDiagnostics
{
    private const string Separator =
        "───────────────────────────────────────────────────────────────";

    public static void Report(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception root = GetRootCause(exception);

        Console.OutputEncoding = Encoding.UTF8;

        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine();
        Console.WriteLine(Separator);
        Console.WriteLine("  ERROR DURANTE LA CONSTRUCCIÓN DEL CONTENEDOR");
        Console.WriteLine(Separator);

        if (IsDependencyInjectionError(root))
        {
            ShowDependencyInjectionError(root);
        }
        else if (IsCircularDependency(root))
        {
            ShowCircularDependency(root);
        }
        else
        {
            Console.WriteLine(" [!] TIPO     : ERROR DE ARRANQUE");
            Console.WriteLine($" [>] EXCEPCIÓN: {root.GetType().Name}");
            Console.WriteLine($" [>] MENSAJE  : {root.Message}");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine();
        Console.WriteLine(" CAUSA RAÍZ");
        Console.WriteLine($" {root.Message}");

        Console.ForegroundColor = previous;
        Console.ResetColor();

        Console.WriteLine(Separator);

        WaitForKey();
    }

    private static void ShowDependencyInjectionError(Exception root)
    {
        string missingType = "No identificado";
        string consumer = "No identificado";

        string[] parts = root.Message.Split('\'');

        if (parts.Length >= 4)
        {
            missingType = CleanTypeName(parts[1]);
            consumer = CleanTypeName(parts[3]);
        }

        Console.WriteLine(" [!] TIPO     : ERROR DE INYECCIÓN DE DEPENDENCIAS");
        Console.WriteLine($" [x] FALTANTE : {missingType}");
        Console.WriteLine($" [x] SOLICITA : {consumer}");

        if (LooksLikeImplementation(missingType))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine(" ANÁLISIS");
            Console.WriteLine(" El constructor está solicitando una implementación.");
            Console.WriteLine(" Si el servicio fue registrado mediante una interfaz,");
            Console.WriteLine(" debe inyectarse la interfaz correspondiente.");
            Console.WriteLine();
            Console.WriteLine($" Tipo sugerido : I{missingType}");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine();
        Console.WriteLine(" PASOS SUGERIDOS");
        Console.WriteLine("   1. Verificar el tipo solicitado en el constructor.");
        Console.WriteLine("   2. Confirmar que el servicio fue registrado.");
        Console.WriteLine("   3. Confirmar que el tipo solicitado coincide con el registrado.");
        Console.WriteLine("   4. Revisar el resumen de Bootstrap.");
    }

    private static void ShowCircularDependency(Exception root)
    {
        Console.WriteLine(" [!] TIPO     : DEPENDENCIA CIRCULAR");

        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine();
        Console.WriteLine(" ANÁLISIS");
        Console.WriteLine(" Dos o más servicios dependen entre sí.");
        Console.WriteLine(" Revise la cadena de constructores.");

        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine();
        Console.WriteLine(" PASOS SUGERIDOS");
        Console.WriteLine("   1. Revisar las dependencias del constructor.");
        Console.WriteLine("   2. Evitar referencias circulares.");
        Console.WriteLine("   3. Extraer la lógica compartida a otro servicio.");
    }

    private static bool IsDependencyInjectionError(Exception ex) =>
        ex.Message.Contains(
            "Unable to resolve service",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsCircularDependency(Exception ex) =>
        ex.Message.Contains(
            "A circular dependency was detected",
            StringComparison.OrdinalIgnoreCase);

    private static Exception GetRootCause(Exception ex)
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

    private static bool LooksLikeImplementation(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        if (typeName.StartsWith('I'))
            return false;

        return typeName.EndsWith("Service", StringComparison.Ordinal);
    }

    [Conditional("DEBUG")]
    private static void WaitForKey()
    {
        if (!Environment.UserInteractive)
            return;

        Console.WriteLine();
        Console.Write("Presione una tecla para finalizar...");
        Console.ReadKey(true);
    }
}
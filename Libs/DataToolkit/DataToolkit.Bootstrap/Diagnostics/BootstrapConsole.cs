using System.Diagnostics;
using System.Text;

namespace DataToolkit.Bootstrap.Diagnostics;

internal static class BootstrapConsole
{
    private const string Separator =
        "----------------------------------------";

    private static readonly StringBuilder Buffer = new(1024);

    static BootstrapConsole()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }

    [Conditional("DEBUG")]
    internal static void Header()
    {
        Buffer.Clear();

        Buffer.AppendLine();
        Buffer.AppendLine(" » INICIANDO: DataToolkit Bootstrap");
        Buffer.AppendLine();
    }

    [Conditional("DEBUG")]
    internal static void Registered(
        Type service,
        Type implementation,
        string lifetime)
    {
        Buffer.Append("🧩 ");
        Buffer.Append(TypeDisplay.GetName(service));
        Buffer.Append(" -> ");
        Buffer.Append(TypeDisplay.GetName(implementation));
        Buffer.Append(" (");
        Buffer.Append(lifetime);
        Buffer.AppendLine(")");
    }

    [Conditional("DEBUG")]
    internal static void Skipped(
        Type implementation,
        string reason)
    {
        Buffer.Append("[SKIP] ");
        Buffer.Append(TypeDisplay.GetName(implementation));
        Buffer.Append(" (");
        Buffer.Append(reason);
        Buffer.AppendLine(")");
    }

    [Conditional("DEBUG")]
    internal static void Error(
        Type implementation,
        Exception exception)
    {
        Buffer.Append("⚠️ [ERROR] ");
        Buffer.AppendLine(TypeDisplay.GetName(implementation));
        Buffer.Append("     ");
        Buffer.AppendLine(exception.Message);
    }

    [Conditional("DEBUG")]
    internal static void Summary(
        int registered,
        int skipped,
        double nanoseconds)
    {
        //Buffer.AppendLine();
        Buffer.AppendLine(Separator);

        Buffer.Append("Registered : ");
        Buffer.Append(registered);
        Buffer.AppendLine();

        Buffer.Append("Skipped    : ");
        Buffer.Append(skipped);
        Buffer.AppendLine();

        Buffer.Append("Elapsed    : ");
        Buffer.Append(FormatElapsed(nanoseconds));
        Buffer.Append(" (");
        Buffer.Append(nanoseconds.ToString("N0"));
        Buffer.AppendLine(" ns)");

        Buffer.AppendLine(Separator);
        Buffer.AppendLine();

        ConsoleColor previousColor = Console.ForegroundColor;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(Buffer);
        Console.ForegroundColor = previousColor;
    }

    private static string FormatElapsed(double nanoseconds)
    {
        if (nanoseconds < 1_000d)
        {
            return string.Concat(
                nanoseconds.ToString("N0"),
                " ns");
        }

        if (nanoseconds < 1_000_000d)
        {
            return string.Concat(
                (nanoseconds / 1_000d).ToString("N2"),
                " µs");
        }

        if (nanoseconds < 1_000_000_000d)
        {
            return string.Concat(
                (nanoseconds / 1_000_000d).ToString("N2"),
                " ms");
        }

        return string.Concat(
            (nanoseconds / 1_000_000_000d).ToString("N2"),
            " s");
    }
}
using System.Diagnostics;
using System.Text;

namespace DataToolkit.Bootstrap.Diagnostics;

internal static class BootstrapConsole
{
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
        string service,
        string implementation,
        string lifetime)
    {
        Buffer.Append("🧩 ");
        Buffer.Append(service);
        Buffer.Append(" -> ");
        Buffer.Append(implementation);
        Buffer.Append(" (");
        Buffer.Append(lifetime);
        Buffer.AppendLine(")");
    }

    [Conditional("DEBUG")]
    internal static void Skipped(
        string implementation,
        string reason)
    {
        Buffer.Append("[SKIP] ");
        Buffer.Append(implementation);
        Buffer.Append(" (");
        Buffer.Append(reason);
        Buffer.AppendLine(")");
    }

    [Conditional("DEBUG")]
    internal static void Error(
        string implementation,
        Exception exception)
    {
        Buffer.Append("⚠️ [ERROR] ");
        Buffer.AppendLine(implementation);
        Buffer.Append("     ");
        Buffer.AppendLine(exception.Message);
    }

    [Conditional("DEBUG")]
    internal static void Summary(
        int registered,
        int skipped,
        double nanoseconds)
    {
        Buffer.AppendLine();
        Buffer.AppendLine("--------------- Summary ----------------");
        Buffer.Append("Registered : ");
        Buffer.AppendLine(registered.ToString());

        Buffer.Append("Skipped    : ");
        Buffer.AppendLine(skipped.ToString());

        Buffer.Append("Elapsed    : ");
        Buffer.Append(FormatElapsed(nanoseconds));
        Buffer.Append(" (");
        Buffer.Append(nanoseconds.ToString("N0"));
        Buffer.AppendLine(" ns)");

        Buffer.AppendLine("----------------------------------------");
        Buffer.AppendLine();

        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.Write(Buffer.ToString());

        Console.ResetColor();
    }

    private static string FormatElapsed(double nanoseconds)
    {
        if (nanoseconds < 1_000)
        {
            return $"{nanoseconds:N0} ns";
        }

        if (nanoseconds < 1_000_000)
        {
            return $"{nanoseconds / 1_000:N2} µs";
        }

        if (nanoseconds < 1_000_000_000)
        {
            return $"{nanoseconds / 1_000_000:N2} ms";
        }

        return $"{nanoseconds / 1_000_000_000:N2} s";
    }
}
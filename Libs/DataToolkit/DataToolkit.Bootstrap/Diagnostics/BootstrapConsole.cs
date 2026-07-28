using System.Diagnostics;
using System.IO;
using System.Text;

namespace DataToolkit.Bootstrap.Diagnostics;

internal static class BootstrapConsole
{
    private const string Banner =
        "▶ DataToolkit Bootstrap v1.0.0";

    private const string Separator =
        "----------------------------------------------------";

    private const int MetricWidth = 20;

    private static readonly StringBuilder Buffer = new(1024);

    static BootstrapConsole()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // La salida puede estar redirigida (CI/CD, pruebas, etc.).
        }
    }

    [Conditional("DEBUG")]
    internal static void Header()
    {
        Buffer.Clear();
        Buffer.AppendLine();
        Buffer.AppendLine(Banner);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Flush();
        Console.ResetColor();
    }

    [Conditional("DEBUG")]
    internal static void Registered(
        Type service,
        Type implementation,
        string lifetime)
    {
        Buffer.Append("✓ [")
              .Append(lifetime)
              .Append("] ")
              .Append(TypeDisplay.GetName(service))
              .Append(" -> ")
              .AppendLine(TypeDisplay.GetName(implementation));
    }

    [Conditional("DEBUG")]
    internal static void Skipped(
        Type implementation,
        string reason)
    {
        Buffer.Append("[SKIP] ")
              .Append(TypeDisplay.GetName(implementation))
              .Append(" (")
              .Append(reason)
              .AppendLine(")");
    }

    [Conditional("DEBUG")]
    internal static void Error(
        Type implementation,
        Exception exception)
    {
        Buffer.Append("⚠ [ERROR] ")
              .AppendLine(TypeDisplay.GetName(implementation))
              .Append("     ")
              .AppendLine(exception.Message);
    }

    [Conditional("DEBUG")]
    internal static void Summary(
        int registered,
        int skipped,
        double nanoseconds)
    {
        Flush();

        WriteSection("Summary");
        WriteMetric("Registered", registered.ToString());
        WriteMetric("Skipped", skipped.ToString());
        WriteMetric("Elapsed", $"{FormatElapsed(nanoseconds)} ({nanoseconds:N0} ns)");

        Console.WriteLine(Separator);
    }

    [Conditional("DEBUG")]
    internal static void Performance(BootstrapProfiler profiler)
    {
        WriteSection($"{"Performance",-MetricWidth} ...... Milliseconds");

        foreach (var (phase, elapsed) in profiler.GetAll())
        {
            WriteMetric(
                GetPhaseName(phase),
                $"{elapsed.TotalMilliseconds:N3} ms");
        }

        WriteMetric(
            "Total",
            $"⚡{profiler.Total.TotalMilliseconds:N3} ms");

        Console.WriteLine(Separator);
    }

    private static void WriteSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(Separator);
    }

    private static void WriteMetric(string name, string value) =>
        Console.WriteLine($"{name,-MetricWidth} ...... {value}");

    private static void Flush()
    {
        if (Buffer.Length == 0)
        {
            return;
        }

        Console.Write(Buffer);
        Buffer.Clear();
    }

    private static string FormatElapsed(double nanoseconds) => nanoseconds switch
    {
        < 1_000d => $"{nanoseconds:N0} ns",
        < 1_000_000d => $"{nanoseconds / 1_000d:N3} µs",
        < 1_000_000_000d => $"{nanoseconds / 1_000_000d:N3} ms",
        _ => $"{nanoseconds / 1_000_000_000d:N3} s"
    };

    private static string GetPhaseName(BootstrapPhase phase) => phase switch
    {
        BootstrapPhase.AssemblyScan => "Assembly Scan",
        BootstrapPhase.Reflection => "Reflection",
        BootstrapPhase.DescriptorBuild => "Preparation",
        BootstrapPhase.DiRegistration => "DI Registration",
        BootstrapPhase.ConsoleOutput => "Console Output",
        _ => phase.ToString()
    };
}
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
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

    private static readonly IReadOnlyDictionary<BootstrapPhase, string> PhaseNames =
        new Dictionary<BootstrapPhase, string>
        {
            [BootstrapPhase.AssemblyScan] = "Assembly Scan",
            [BootstrapPhase.Reflection] = "Reflection",
            [BootstrapPhase.DescriptorBuild] = "Preparation",
            [BootstrapPhase.DiRegistration] = "DI Registration",
            [BootstrapPhase.ConsoleOutput] = "Console Output"
        };

    static BootstrapConsole()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }

    [Conditional("DEBUG")]
    internal static void Header()
    {
        Buffer.Clear();
        Buffer.AppendLine();
        Buffer.AppendLine(Banner);
        //Buffer.AppendLine();
        
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
        Buffer.Append("✓ ");
        Buffer.Append('[');
        Buffer.Append(lifetime);
        Buffer.Append("] ");
        Buffer.Append(TypeDisplay.GetName(service));
        Buffer.Append(" -> ");
        Buffer.AppendLine(TypeDisplay.GetName(implementation));
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
        Buffer.Append("⚠ [ERROR] ");
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
        Flush();

        WriteSection("Summary");

        WriteMetric(
            "Registered",
            registered.ToString());

        WriteMetric(
            "Skipped",
            skipped.ToString());

        WriteMetric(
            "Elapsed",
            $"{FormatElapsed(nanoseconds)} ({nanoseconds:N0} ns)");

        Console.WriteLine(Separator);
    }

    [Conditional("DEBUG")]
    internal static void Performance(
        BootstrapProfiler profiler)
    {
        WriteSection($"{"Performance",-MetricWidth} ...... Milliseconds");

        foreach (KeyValuePair<BootstrapPhase, TimeSpan> phase in profiler.GetAll())
        {
            WriteMetric(
                GetPhaseName(phase.Key),
                $"{phase.Value.TotalMilliseconds:N3} ms");
        }

        WriteMetric(
            "Total",
            $"{profiler.Total.TotalMilliseconds:N3} ms");

        Console.WriteLine(Separator);
    }

    private static void WriteSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(Separator);
    }

    private static void WriteMetric(
        string name,
        string value)
    {
        Console.WriteLine($"{name,-MetricWidth} ...... {value}");
    }

    private static void Flush()
    {
        if (Buffer.Length == 0)
        {
            return;
        }

        Console.Write(Buffer);
        Buffer.Clear();
    }

    private static string FormatElapsed(double nanoseconds)
    {
        if (nanoseconds < 1_000d)
        {
            return $"{nanoseconds:N0} ns";
        }

        if (nanoseconds < 1_000_000d)
        {
            return $"{nanoseconds / 1_000d:N3} µs";
        }

        if (nanoseconds < 1_000_000_000d)
        {
            return $"{nanoseconds / 1_000_000d:N3} ms";
        }

        return $"{nanoseconds / 1_000_000_000d:N3} s";
    }

    private static string GetPhaseName(BootstrapPhase phase)
    {
        return PhaseNames.TryGetValue(
            phase,
            out string? name)
            ? name
            : phase.ToString();
    }
}
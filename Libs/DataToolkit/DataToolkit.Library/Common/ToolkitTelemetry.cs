using System.Diagnostics;
using Microsoft.Extensions.Logging; //using Serilog;

namespace DataToolkit.Library.Common;

internal static class ToolkitTelemetry
{
    public static Stopwatch? Start(bool enabled)
        => enabled ? Stopwatch.StartNew() : null;

    public static void Stop(
        ILogger logger,
        DataToolkitOptions opt,
        string action,
        Stopwatch? sw)
    {
        if (!opt.Logging.Enabled || sw == null) return;

        sw.Stop();
        var ms = sw.ElapsedMilliseconds;

        var prefix = opt.Logging.Prefix;

        if (opt.Telemetry.Enabled && ms >= opt.Telemetry.SlowMs)
            //logger.Warning("[{Prefix}] SLOW: {Action} ({Ms}ms)", prefix, action, ms);
            logger.LogWarning("[{Prefix}] SLOW: {Action} ({Ms}ms)", prefix, action, ms);
        else
            logger.LogInformation("[{Prefix}] {Action} ({Ms}ms)", prefix, action, ms);
        //logger.Information("[{Prefix}] {Action} ({Ms}ms)", prefix, action, ms);
    }

    public static void Error(
        ILogger logger,
        DataToolkitOptions opt,
        string action,
        Exception ex,
        Stopwatch? sw)
    {
        sw?.Stop();

        //logger.Error(ex, "[{Prefix}] ERROR: {Action}", opt.Logging.Prefix, action);
        logger.LogError(ex, "[{Prefix}] ERROR: {Action}", opt.Logging.Prefix, action);
    }
}
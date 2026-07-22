namespace DataToolkit.Library.Common;

public sealed class DataToolkitOptions
{
    public const string SectionName = "DataToolkit";

    public string DefaultConnectionAlias { get; set; }
        = "SqlServer";

    public LoggingOptions Logging { get; set; } = new();
    public TelemetryOptions Telemetry { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();
}
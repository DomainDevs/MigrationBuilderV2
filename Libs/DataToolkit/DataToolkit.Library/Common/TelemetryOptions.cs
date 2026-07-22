namespace DataToolkit.Library.Common;

public sealed class TelemetryOptions
{
    public bool Enabled { get; set; } = true;
    public int SlowMs { get; set; } = 500;
}
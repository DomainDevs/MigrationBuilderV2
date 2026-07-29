namespace Persistence.Execution.Log;

public sealed class LogEntry
{
    public string Name { get; set; } = string.Empty;

    public bool Ok { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public string Msg { get; set; } = string.Empty;

    public double Elapsed => (End - Start).TotalSeconds;

    public string Status => Ok ? "Success" : "Failed";
}
namespace Persistence.Execution.Log;

public sealed class LogEntry
{
    public string Name { get; set; } = string.Empty;

    public bool Ok { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public string Message { get; set; } = string.Empty;

    public double Duration => (End - Start).TotalSeconds;

    public string Status => Ok ? "Success" : "Failed";
}
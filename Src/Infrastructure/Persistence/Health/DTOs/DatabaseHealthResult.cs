namespace Persistence.Health.DTOs;

public sealed class DatabaseHealthResult
{
    public string Name { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public string? Server { get; init; }
    public string? Database { get; init; }
    public HealthStatus? Ping { get; set; }
    public HealthStatus DatabaseStatus { get; set; }
    public string? Error { get; set; }
}

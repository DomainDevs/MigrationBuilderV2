using System.Text.Json.Serialization;

namespace Persistence.Health.DTOs;

public sealed class DatabaseHealthResult
{
    public string Name { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public string? Server { get; init; }
    public string? Database { get; init; }
    [JsonIgnore]
    public HealthStatus? Ping { get; set; }

    [JsonPropertyName("ping")]
    public string? PingText => Ping?.ToString();

    [JsonIgnore]
    public HealthStatus DatabaseStatus { get; set; }
    [JsonPropertyName("databaseStatus")]
    public string DatabaseStatusText => DatabaseStatus.ToString();
    public string? Error { get; set; }
}

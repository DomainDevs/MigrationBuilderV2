using Microsoft.Extensions.Configuration;

namespace Persistence.Configuration;

public static class ConfigurationExtensions
{
    public static Dictionary<string, string?> GetMigrationConnectionStrings(
        this IConfiguration configuration)
    {
        configuration.GetSection("SourceDB").Get<ConnectionConfig>();
        configuration.GetSection("DestinationDB").Get<ConnectionConfig>();

        var source = configuration.GetSection("SourceDB").Get<ConnectionConfig>();
        var target = configuration.GetSection("DestinationDB").Get<ConnectionConfig>();

        return new Dictionary<string, string?>
        {
            ["Source"] = source?.BuildConnectionStringSql(),
            ["Target"] = target?.BuildConnectionStringSql(),
            ["Workspace"] = configuration["Workspace:BaseDatos"]
        };
    }
}
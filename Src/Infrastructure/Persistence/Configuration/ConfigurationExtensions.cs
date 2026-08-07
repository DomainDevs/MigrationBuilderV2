using Microsoft.Extensions.Configuration;

namespace Persistence.Configuration;

public static class ConfigurationExtensions
{
    public static Dictionary<string, string?> GetMigrationConnectionStrings(
        this IConfiguration configuration)
    {
        Dictionary<string, string?> connectionStrings = new();

        foreach (IConfigurationSection section in configuration
            .GetSection("Connections")
            .GetChildren())
        {
            ConnectionConfig? connection =
                section.Get<ConnectionConfig>();

            if (connection is null)
                continue;

            string connectionString = connection.Provider switch
            {
                "SqlServer" => connection.BuildConnectionStringSql(),

                "Sqlite" => connection.Database,

                _ => throw new NotSupportedException(
                    $"Provider '{connection.Provider}' is not supported.")
            };

            connectionStrings.Add(
                $"ConnectionStrings:{section.Key}",
                connectionString);
        }

        /*
        Console.WriteLine("========== ConnectionStrings ==========");
        foreach (var item in connectionStrings)
        {
            Console.WriteLine($"{item.Key}");
            Console.WriteLine($"    {item.Value}");
            Console.WriteLine();
        }
        Console.WriteLine("=======================================");
        */

        return connectionStrings;
    }
}
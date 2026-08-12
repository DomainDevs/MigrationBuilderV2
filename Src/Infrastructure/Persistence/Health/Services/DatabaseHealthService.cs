using DataToolkit.Library.Connections.Context;
using Microsoft.Extensions.Configuration;
using Persistence.Health.DTOs;
using System.Net.NetworkInformation;

namespace Persistence.Health.Services;

public sealed class DatabaseHealthService
{
    private readonly IDatabaseContext _database;
    private readonly IConfiguration _configuration;

    public DatabaseHealthService(
        IDatabaseContext database,
        IConfiguration configuration)
    {
        _database = database;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<DatabaseHealthResult>> CheckAsync()
    {
        List<DatabaseHealthResult> results = [];

        IConfigurationSection connections =
            _configuration.GetSection("Connections");

        foreach (IConfigurationSection connection in connections.GetChildren())
        {
            string name = connection.Key;

            string? provider =
                connection["Provider"];

            string? server =
                connection["Server"];

            string? database =
                connection["Database"];

            DatabaseHealthResult result =
                new()
                {
                    Name = name,
                    Provider = provider,
                    Server = server,
                    Database = database
                };

            if (!string.IsNullOrWhiteSpace(server))
            {
                result.Ping =
                    await PingAsync(server);

                if (result.Ping != HealthStatus.Healthy)
                {
                    result.DatabaseStatus =
                        HealthStatus.Unhealthy;

                    result.Error =
                        "El servidor no responde.";

                    results.Add(result);

                    continue;
                }
            }

            try
            {
                using var databaseContext =
                    _database[name].CreateNew();

                int value =
                    (await databaseContext.Sql.FromSqlAsync<int>("SELECT 1", commandTimeout:3))
                    .Single();

                result.DatabaseStatus =
                    value == 1
                        ? HealthStatus.Healthy
                        : HealthStatus.Unhealthy;
            }
            catch (Exception ex)
            {
                result.DatabaseStatus =
                    HealthStatus.Unhealthy;

                result.Error =
                    ex.Message;
            }

            results.Add(result);
        }

        return results;
    }

    private static async Task<HealthStatus> PingAsync(
        string server)
    {
        using System.Net.NetworkInformation.Ping ping = new();

        try
        {
            PingReply reply =
                await ping.SendPingAsync(
                    server,
                    3000);

            return reply.Status == IPStatus.Success
                ? HealthStatus.Healthy
                : HealthStatus.Unhealthy;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }
}
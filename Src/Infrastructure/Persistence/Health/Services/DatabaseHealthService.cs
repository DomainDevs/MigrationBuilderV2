using DataToolkit.Library.Connections.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Persistence.Health.DTOs;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Persistence.Health.Services;

public sealed class DatabaseHealthService
{
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DatabaseTimeout = TimeSpan.FromSeconds(5);

    private readonly IDatabaseContext _database;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(
        IDatabaseContext database,
        IConfiguration configuration,
        ILogger<DatabaseHealthService> logger)
    {
        _database = database;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DatabaseHealthResult>> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        List<DatabaseHealthResult> results = [];

        IConfigurationSection connections =
            _configuration.GetSection("Connections");

        foreach (IConfigurationSection connection in connections.GetChildren())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string name = connection.Key;
            string? server = connection["Server"];
            string? database = connection["Database"];

            DatabaseHealthResult result = new()
            {
                Name = name,
                Server = server,
                Database = database
            };

            using CancellationTokenSource timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutCts.CancelAfter(DatabaseTimeout);

            Stopwatch stopwatch = Stopwatch.StartNew();
            Exception? failure = null;

            try
            {
                // Ping is diagnostic only; it does not determine SQL availability.
                if (!string.IsNullOrWhiteSpace(server))
                {
                    result.Ping = await PingAsync(
                        server,
                        PingTimeout,
                        timeoutCts.Token);
                }

                timeoutCts.Token.ThrowIfCancellationRequested();

                using var databaseContext =
                    _database[name].CreateNew();

                timeoutCts.Token.ThrowIfCancellationRequested();

                int value =
                    (await databaseContext.Sql.FromSqlAsync<int>(
                        "SELECT 1",
                        commandTimeout: 3,
                        ct: timeoutCts.Token))
                    .Single();

                result.DatabaseStatus = value == 1
                    ? HealthStatus.Healthy
                    : HealthStatus.Unhealthy;

                if (value != 1)
                {
                    result.Error =
                        "La consulta de comprobación no devolvió el resultado esperado.";
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                failure = ex;
                result.DatabaseStatus = HealthStatus.Unhealthy;
                result.Error =
                    "La comprobación superó el tiempo máximo de espera.";
            }
            catch (Exception ex)
            {
                failure = ex;
                result.DatabaseStatus = HealthStatus.Unhealthy;
                result.Error = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
            }

            if (result.DatabaseStatus == HealthStatus.Healthy)
            {
                _logger.LogInformation(
                    "Database health check succeeded. Name={Name}, Server={Server}, Database={Database}, Ping={Ping}, DurationMs={DurationMs}",
                    name,
                    server ?? "(not configured)",
                    database ?? "(not configured)",
                    result.Ping?.ToString() ?? "NotChecked",
                    stopwatch.ElapsedMilliseconds);
            }
            else if (failure is not null)
            {
                _logger.LogError(
                    failure,
                    "Database health check failed. Name={Name}, Server={Server}, Database={Database}, Ping={Ping}, DurationMs={DurationMs}, Error={Error}",
                    name,
                    server ?? "(not configured)",
                    database ?? "(not configured)",
                    result.Ping?.ToString() ?? "NotChecked",
                    stopwatch.ElapsedMilliseconds,
                    result.Error);
            }
            else
            {
                _logger.LogWarning(
                    "Database health check unhealthy. Name={Name}, Server={Server}, Database={Database}, Ping={Ping}, DurationMs={DurationMs}, Error={Error}",
                    name,
                    server ?? "(not configured)",
                    database ?? "(not configured)",
                    result.Ping?.ToString() ?? "NotChecked",
                    stopwatch.ElapsedMilliseconds,
                    result.Error ?? "Unknown health check failure");
            }

            results.Add(result);
        }

        return results;
    }

    private static async Task<HealthStatus> PingAsync(
        string server,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using Ping ping = new();

        try
        {
            PingReply reply = await ping
                .SendPingAsync(server, (int)timeout.TotalMilliseconds)
                .WaitAsync(timeout, cancellationToken);

            return reply.Status == IPStatus.Success
                ? HealthStatus.Healthy
                : HealthStatus.Unhealthy;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }
}

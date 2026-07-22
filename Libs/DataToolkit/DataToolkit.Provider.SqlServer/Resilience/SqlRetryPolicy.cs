using DataToolkit.Library.Extensions.Resilience;
using Microsoft.Data.SqlClient;

namespace DataToolkit.Provider.SqlServer.Resilience;

public sealed class SqlRetryPolicy : IRetryPolicy
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;
    private readonly bool _retryEnabled;
   

    public SqlRetryPolicy(bool retryEnabled = false, int maxRetries = 3, int baseDelayMs = 200)
    {
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
        _retryEnabled = retryEnabled;
    }

    public bool ShouldRetry(Exception ex, int attempt)
    {
        if (attempt >= _maxRetries)
            return false;

        if (ex is not SqlException sqlEx)
            return false;

        return sqlEx.Number switch
        {
            // 1205
            // Deadlock victim.
            // SQL Server eligió esta transacción como víctima
            // para resolver un interbloqueo.
            // Retry: SI.
            1205 => true,

            // 40613
            // Database unavailable.
            // La base de datos no está disponible temporalmente.
            // Común durante failover o recuperación.
            // Retry: SI.
            40613 => true,

            // 40197
            // Azure SQL Database experimentó un error transitorio.
            // Suele ocurrir durante failover, mantenimiento
            // o reconfiguración interna del servicio.
            // Retry: SI.
            40197 => true,

            // 40501
            // Service Busy.
            // Azure SQL está limitando temporalmente solicitudes
            // por alta carga o throttling.
            // Retry: SI.
            40501 => true,

            // 10928
            // Resource limit reached.
            // Se alcanzó el límite de recursos del plan
            // (DTU, CPU, Workers, etc.).
            // Retry: SI.
            10928 => true,

            // 10929
            // Too many requests.
            // SQL Azure está rechazando solicitudes debido
            // a presión de recursos o exceso de concurrencia.
            // Retry: SI.
            10929 => true,

            _ => false
        };
    }

    public TimeSpan GetDelay(int attempt)
    {
        var ms =
            _baseDelayMs * Math.Pow(2, attempt - 1);

        return TimeSpan.FromMilliseconds(ms);
    }
}
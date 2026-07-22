namespace DataToolkit.Library.Common;

public sealed class RetryOptions
{
    /// <summary>
    /// Habilita o deshabilita la resiliencia.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Número máximo de reintentos.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay base en milisegundos (backoff exponencial).
    /// </summary>
    public int BaseDelayMs { get; set; } = 200;
}
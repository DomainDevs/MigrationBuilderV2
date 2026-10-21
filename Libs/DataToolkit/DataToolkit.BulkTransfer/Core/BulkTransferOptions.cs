namespace DataToolkit.BulkTransfer.Core;
/// <summary>
/// Configuración de la transferencia masiva de datos.
/// </summary>
public class BulkTransferOptions
{
    /// <summary>Filas procesadas por lote.</summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>Tiempo máximo de espera en segundos.</summary>
    public int Timeout { get; set; } = 600;

    /// <summary>Habilita la transferencia directa.</summary>
    public Boolean DirectTransfer { get; set; } = false;

    /// <summary>
    /// Solicita bloqueo de tabla; puede favorecer el registro mínimo y reducir el log.
    /// </summary>
    public bool TableLock { get; set; } = false;
}
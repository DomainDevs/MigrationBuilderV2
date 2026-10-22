using Persistence.Execution.Log;
using System.Text.Json;

namespace Persistence.Execution.Helpers;

public sealed class LogWriterJSON
{
    private readonly string _carpetaBase;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LogWriterJSON(string carpetaBase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carpetaBase);

        _carpetaBase = Path.GetFullPath(carpetaBase);

        Directory.CreateDirectory(_carpetaBase);
    }

    /// <summary>
    /// Escribe el registro de ejecución de un paquete en un archivo JSON
    /// independiente. No elimina logs anteriores.
    /// </summary>
    /// <param name="nombreArchivo">Nombre del paquete o proceso, sin extensión.</param>
    /// <param name="entradas">Registros de los artefactos ejecutados.</param>
    public void EscribirLog(
        string nombreArchivo,
        List<LogEntry> entradas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreArchivo);
        ArgumentNullException.ThrowIfNull(entradas);

        if (entradas.Count == 0)
            return;

        Directory.CreateDirectory(_carpetaBase);

        string nombreSeguro = SanitizeFileName(nombreArchivo);

        // Incluye fracciones de segundo para reducir colisiones
        // cuando varios paquetes terminan concurrentemente.
        string timestamp = DateTime.UtcNow.ToString(
            "yyyyMMdd_HHmmss_fffffff");

        string archivo = Path.Combine(
            _carpetaBase,
            $"{nombreSeguro}_{timestamp}.json");

        string archivoTemporal = Path.Combine(
            _carpetaBase,
            $".{nombreSeguro}_{Guid.NewGuid():N}.tmp");

        DateTime createdAt = DateTime.Now;

        var reporte = new
        {
            Job = nombreArchivo,
            CreatedAt = createdAt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            StepCount = entradas.Count,
            Success = entradas.Count(entry => entry.Ok),
            Failed = entradas.Count(entry => !entry.Ok),

            Steps = entradas.Select(entry => new
            {
                entry.Name,
                Start = entry.Start.ToString("HH:mm:ss.fff"),
                End = entry.End.ToString("HH:mm:ss.fff"),
                Elapsed = Math.Max(
                    0,
                    (entry.End - entry.Start).TotalSeconds),
                entry.Ok,
                entry.Msg
            }).ToList()
        };

        try
        {
            string json = JsonSerializer.Serialize(
                reporte,
                JsonOptions);

            // Escribir primero un archivo temporal en la misma carpeta.
            File.WriteAllText(archivoTemporal, json);

            // Publicar el reporte solo cuando el JSON esté completo.
            File.Move(archivoTemporal, archivo);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                $"Reporte JSON generado: {archivo}");
            Console.ResetColor();
        }
        finally
        {
            // Si falla la escritura, limpiar el temporal cuando sea posible.
            if (File.Exists(archivoTemporal))
                File.Delete(archivoTemporal);
        }
    }

    private static string SanitizeFileName(string nombreArchivo)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();

        HashSet<char> invalid = new(invalidChars)
        {
            '/',
            '\\'
        };

        string result = new(
            nombreArchivo
                .Select(character =>
                    invalid.Contains(character) ||
                    char.IsControl(character)
                        ? '_'
                        : character)
                .ToArray());

        result = result.Trim().Trim('.');

        return string.IsNullOrWhiteSpace(result)
            ? "Execution"
            : result;
    }
}

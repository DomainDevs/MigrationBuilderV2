using Persistence.Execution.Log;
using System.Text.Json;

namespace Persistence.Execution.Helpers;


public class LogWriterJSON
{
    private readonly string _carpetaBase;

    public LogWriterJSON(string carpetaBase)
    {
        if (string.IsNullOrWhiteSpace(carpetaBase))
            throw new ArgumentException("Debe indicar una carpeta base para los logs.", nameof(carpetaBase));

        _carpetaBase = carpetaBase;

        if (!Directory.Exists(_carpetaBase))
            Directory.CreateDirectory(_carpetaBase);
    }

    /// <summary>
    /// Escribe el log en un archivo JSON
    /// </summary>
    /// <param name="nombreArchivo">Nombre del archivo sin extensión</param>
    /// <param name="entradas">Lista de logs a escribir</param>
    public void EscribirLog(string nombreArchivo, List<LogEntry> entradas)
    {
        if (entradas == null || entradas.Count == 0)
            return; // No hay nada que escribir

        string archivo = Path.Combine(_carpetaBase, $"{nombreArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        // Obtenemos el nombre del Excel de la primera entrada si existe
        //string excelPath = entradas.Count > 0 ? entradas[0].FileXLS ?? "" : "";

        var reporte = new
        {
            Job = nombreArchivo,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            StepCount = entradas.Count,
            Success = entradas.Count(e => e.Ok),
            Failed = entradas.Count(e => !e.Ok),
            //ExcelPath = excelPath, // Solo en la raíz
            Steps = entradas.ConvertAll(e => new
            {
                e.Name,
                Start = e.Start.ToString("HH:mm:ss.fff"),
                End = e.End.ToString("HH:mm:ss.fff"),
                Elapsed = (e.End - e.Start).TotalSeconds,
                e.Ok,
                e.Msg
                // 🔹 Aquí ya no ponemos FileXLS
            })
        };

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        string json = JsonSerializer.Serialize(reporte, jsonOptions);
        File.WriteAllText(archivo, json);

        //Limpiar
        DeleteOldFiles();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nReporte JSON generado: {archivo}");
        Console.ResetColor();
    }
    private void DeleteOldFiles()
    {
        FileInfo[] files =
            new DirectoryInfo(_carpetaBase)
                .GetFiles("*.json")
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToArray();

        foreach (FileInfo file in files.Skip(10))
        {
            file.Delete();
        }
    }

}
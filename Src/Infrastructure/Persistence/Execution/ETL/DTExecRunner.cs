using Persistence.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Persistence.Execution.ETL;

public static class DTExecRunner
{
    public static void EjecutarPaqueteETL(
        string rutaPaquete,
        ConnectionConfig source,
        ConnectionConfig target)
    {
        if (string.IsNullOrWhiteSpace(rutaPaquete) || !File.Exists(rutaPaquete))
            throw new FileNotFoundException($"No se encontró el paquete: {rutaPaquete}");

        string sourceConnection = source.BuildConnectionStringETL();
        string destinationConnection = target.BuildConnectionStringETL();

        var proceso = new Process();

        proceso.StartInfo.FileName = FindDTExec();

        proceso.StartInfo.Arguments =
            $"/F \"{rutaPaquete}\" " +
            $"/CONN \"SourceDB\";\"{sourceConnection}\" " +
            $"/CONN \"DestinationDB\";\"{destinationConnection}\"";

        proceso.StartInfo.UseShellExecute = false;
        proceso.StartInfo.RedirectStandardOutput = true;
        proceso.StartInfo.RedirectStandardError = true;
        proceso.StartInfo.CreateNoWindow = true;

        proceso.Start();

        string output = proceso.StandardOutput.ReadToEnd();
        string error = proceso.StandardError.ReadToEnd();

        proceso.WaitForExit();

        if (proceso.ExitCode != 0)
        {
            string mensaje = ExtraerMensajeError(error, output);

            throw new Exception(mensaje);
        }
    }

    private static string ExtraerMensajeError(string error, string output)
    {
        string contenido = string.Join(
            Environment.NewLine,
            new[] { error, output }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        if (string.IsNullOrWhiteSpace(contenido))
            return "DTExec finalizó con error, pero no devolvió información adicional.";

        // Busca las líneas Description: de los errores reales de SSIS.
        MatchCollection errores = Regex.Matches(
            contenido,
            @"Description:\s*(.+)",
            RegexOptions.IgnoreCase);

        List<string> mensajes = [];

        foreach (Match match in errores)
        {
            string mensaje = match.Groups[1].Value.Trim();

            if (string.IsNullOrWhiteSpace(mensaje))
                continue;

            if (!mensajes.Contains(mensaje, StringComparer.OrdinalIgnoreCase))
                mensajes.Add(mensaje);
        }

        if (mensajes.Count > 0)
            return string.Join(Environment.NewLine, mensajes);

        // Fallback: si DTExec no devuelve Description:, conservamos
        // el contenido para no perder información.
        return contenido.Trim();
    }

    private static string FindDTExec()
    {
        var root = @"C:\Program Files\Microsoft SQL Server";

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(root);

        var candidates = Directory
            .GetDirectories(root)
            .Select(x => Path.Combine(x, "DTS", "Binn", "DTExec.exe"))
            .Where(File.Exists)
            .OrderByDescending(x => x)
            .ToList();

        if (!candidates.Any())
            throw new FileNotFoundException("No se encontró DTExec.exe");

        return candidates.First();
    }
}
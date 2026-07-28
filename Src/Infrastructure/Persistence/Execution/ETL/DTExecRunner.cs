using Microsoft.Extensions.Configuration;
using Persistence.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Execution.ETL;


public static class DTExecRunner
{
    public static void EjecutarPaqueteETL(string rutaPaquete,
        ConnectionConfig source, ConnectionConfig target)
    {
        if (string.IsNullOrWhiteSpace(rutaPaquete) || !File.Exists(rutaPaquete))
            throw new FileNotFoundException($"No se encontró el paquete: {rutaPaquete}");

        string sourceConnection = source.BuildConnectionStringETL();
        string destinationConnection = target.BuildConnectionStringETL();

        string carpetaPaquete = Path.GetDirectoryName(rutaPaquete)!;

        // ======== Buscar archivos que contengan "Source" o "Destination" ========
        var proceso = new System.Diagnostics.Process();
        proceso.StartInfo.FileName = FindDTExec(); //proceso.StartInfo.FileName = "dtexec.exe";

        proceso.StartInfo.Arguments =
            $"/F \"{rutaPaquete}\" " +
            $"/CONN \"SourceDB\";\"{sourceConnection}\" " +
            $"/CONN \"DestinationDB\";\"{destinationConnection}\"";

        proceso.StartInfo.UseShellExecute = false;
        proceso.StartInfo.RedirectStandardOutput = true;
        proceso.StartInfo.RedirectStandardError = true;
        proceso.StartInfo.CreateNoWindow = true;

        //Console.WriteLine(proceso.StartInfo.Arguments);

        proceso.Start();


        string output = proceso.StandardOutput.ReadToEnd();
        string error = proceso.StandardError.ReadToEnd();

        proceso.WaitForExit();

        if (proceso.ExitCode != 0)
            throw new Exception($"Error al ejecutar paquete: {error}\n{output}");
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

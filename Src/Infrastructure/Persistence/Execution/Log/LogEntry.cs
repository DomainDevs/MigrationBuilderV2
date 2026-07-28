namespace Persistence.Execution.Log;

public class LogEntry
{
    public string NombrePaso { get; set; }
    public bool Exito { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string Mensaje { get; set; }
    public double DuracionSegundos => (Fin - Inicio).TotalSeconds; // Resultado resumido del Job (solo para JSON/reportes)
    public string Status => Exito ? "Success" : "Failed";
}
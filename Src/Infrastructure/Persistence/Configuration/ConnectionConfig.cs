namespace Persistence.Configuration;

public class ConnectionConfig
{
    public string Provider { get; set; } = "MSOLEDBSQL.1";
    public string Servidor { get; set; } = string.Empty;
    public string BaseDatos { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool PersistSecurityInfo { get; set; } = true;

    public string BuildConnectionStringETL()
    {
        return
            $"Provider={Provider};" +
            $"Data Source={Servidor};" +
            $"Initial Catalog={BaseDatos};" +
            $"User ID={Usuario};" +
            $"Password={Password};" +
            $"Persist Security Info={PersistSecurityInfo};";
    }
    public string BuildConnectionStringSql()
    {
        return
            $"Server={Servidor};" +
            $"Database={BaseDatos};" +
            $"User Id={Usuario};" +
            $"Password={Password};" +
            $"Pooling=true;" +
            $"Min Pool Size=3;" +
            $"Max Pool Size=30;" +
            $"Connection Timeout=15;" +
            $"Application Name=LIB;" +
            $"Language=us_english;" +
            $"Encrypt=True;" +
            $"TrustServerCertificate=True;";
    }

}

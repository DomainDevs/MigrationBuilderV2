namespace Persistence.Configuration;

public sealed class ConnectionConfig
{
    public string Provider { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool PersistSecurityInfo { get; set; }
    public int TimeOut { get; set; }

    public string BuildConnectionStringETL()
    {
        return
            $"Provider={Driver};" +
            $"Data Source={Server};" +
            $"Initial Catalog={Database};" +
            $"User ID={User};" +
            $"Password={Password};" +
            $"Persist Security Info={PersistSecurityInfo};"+
            $"Connect Timeout={TimeOut};";
    }
    public string BuildConnectionStringSql()
    {
        return
            $"Server={Server};" +
            $"Database={Database};" +
            $"User Id={User};" +
            $"Password={Password};" +
            $"Pooling=true;" +
            $"Min Pool Size=3;" +
            $"Max Pool Size=30;" +
            $"Connection Timeout={TimeOut};" +
            $"Application Name=LIB;" +
            $"Language=us_english;" +
            $"Encrypt=True;" +
            $"TrustServerCertificate=True;";
    }

}

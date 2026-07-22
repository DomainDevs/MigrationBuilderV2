using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Persistence.Database;


public sealed class SqliteDatabaseInitializer
{
    private readonly IConfiguration _configuration;

    public SqliteDatabaseInitializer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        var dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Workspace.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        await using var connection =
            new SqliteConnection($"Data Source={dbPath}");

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = GetCreateDatabaseScript();

        await command.ExecuteNonQueryAsync();
    }

    private static string GetCreateDatabaseScript()
    {
        return """
        CREATE TABLE IF NOT EXISTS PackageExecution
        (
            ExecutionId INTEGER PRIMARY KEY AUTOINCREMENT,
            FileId      TEXT NOT NULL,
            FileName    TEXT NOT NULL,
            Status      TEXT NOT NULL CHECK (
                                Status IN (
                                    'Pending',
                                    'Running',
                                    'Completed',
                                    'Failed',
                                    'Skipped'
                                )
                            ),
            StartTime   TEXT NULL,
            EndTime     TEXT NULL,
            DurationMs  INTEGER NULL,
            Message     TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_PackageExecution_FileId
            ON PackageExecution(FileId);

        CREATE INDEX IF NOT EXISTS IX_PackageExecution_Status
            ON PackageExecution(Status);
        """;
    }
}
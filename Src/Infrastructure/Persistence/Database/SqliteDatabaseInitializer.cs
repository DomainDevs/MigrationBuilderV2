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
        
        CREATE TABLE IF NOT EXISTS PackageExecutionStep
        (
            StepExecutionId INTEGER PRIMARY KEY AUTOINCREMENT,

            ExecutionId     INTEGER      NOT NULL,

            StepName        TEXT         NOT NULL,

            Status          TEXT         NOT NULL
                CHECK (Status IN
                (
                    'Pending',
                    'Running',
                    'Completed',
                    'Failed',
                    'Skipped'
                )),

            StartTime       DATETIME     NULL,
            EndTime         DATETIME     NULL,
            DurationMs      INTEGER      NULL,

            Message         TEXT         NULL,

            FOREIGN KEY (ExecutionId)
                REFERENCES PackageExecution (ExecutionId)
                ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_PackageExecutionStep_ExecutionId
            ON PackageExecutionStep (ExecutionId);

        CREATE INDEX IF NOT EXISTS IX_PackageExecutionStep_Status
            ON PackageExecutionStep (Status);

        CREATE TABLE IF NOT EXISTS tuser (
            id_user         INTEGER PRIMARY KEY AUTOINCREMENT,
            user_name       TEXT NOT NULL UNIQUE,
            password_hash   TEXT NOT NULL,
            enabled         INTEGER NOT NULL DEFAULT 1,
            created_at      TEXT NOT NULL,
            updated_at      TEXT NULL
        );

        INSERT INTO tuser
        (
            user_name,
            password_hash,
            enabled,
            created_at
        )
        SELECT
            'admin',
            'admin',
            1,
            datetime('now')
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM tuser
            WHERE user_name = 'admin'
        );

        """;
    }
}
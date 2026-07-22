using Persistence.Configuration;

namespace App.Configurations;

internal static class Startup
{
    private const string ConfigDirectory = "Configurations";

    internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
    {
        IHostEnvironment env = builder.Environment;
        DirectoryInfo directory = new DirectoryInfo(ConfigDirectory);

        if (directory.Exists)
        {
            foreach (FileInfo file in directory.EnumerateFiles("*.json"))
            {
                string baseName = Path.GetFileNameWithoutExtension(file.Name);
                string extension = Path.GetExtension(file.Name);

                // Archivo base
                builder.Configuration.AddJsonFile(
                    Path.Combine(ConfigDirectory, file.Name),
                    optional: false,
                    reloadOnChange: true);

                // Archivo específico por entorno
                builder.Configuration.AddJsonFile(
                    Path.Combine(ConfigDirectory, $"{baseName}.{env.EnvironmentName}{extension}"),
                    optional: true,
                    reloadOnChange: true);
            }
        }

        // Variables de entorno
        builder.Configuration.AddEnvironmentVariables();

        // ConnectionStrings generadas en memoria
        builder.Configuration.AddInMemoryCollection(
            builder.Configuration.GetMigrationConnectionStrings());

        return builder;
    }
}
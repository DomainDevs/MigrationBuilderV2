using App.Configurations;
using Application;
using Infrastructure;
using Infrastructure.Common.Diagnostics;
using Persistence;
using Serilog;

try
{
    WebApplicationBuilder builder 
        = WebApplication.CreateBuilder(args);

    Log.Information("Servicio iniciando...");

    // ---------------------------------------------------------------------
    // Configuration
    // ---------------------------------------------------------------------
    builder.AddConfigurations();
    builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

    // ---------------------------------------------------------------------
    // Service Registration (Manual)
    // ---------------------------------------------------------------------
    bool isDev = builder.Environment.IsDevelopment();
    builder.Services
        .AddInfrastructure(builder.Configuration, isDev)
        .AddPersistence(builder.Configuration)
        .AddApplication();

    // ---------------------------------------------------------------------
    // Service Registration (Automatic)
    // ---------------------------------------------------------------------
    builder.Services.AddBootstrap(
        (typeof(Application.AssemblyReference).Assembly, "Application.Features","Services"),
        (typeof(Persistence.AssemblyReference).Assembly, "Persistence", "Repositories")
        );

    builder.Services.AddControllers();

    // ---------------------------------------------------------------------
    // Build
    // ---------------------------------------------------------------------
    var app = builder.Build();

    // ---------------------------------------------------------------------
    // Middleware / Startup
    // ---------------------------------------------------------------------
    app.UseInfrastructure(builder.Configuration, isDev);
    await app.UsePersistenceAsync();

    // ---------------------------------------------------------------------
    // Endpoints
    // ---------------------------------------------------------------------
    app.MapControllers();

    Log.Information("Servicio iniciando correctamente...");
    app.Run();

}
catch (Exception ex) when (!ex.GetType()
    .Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    Log.Information(">> Servicio finalizado, error critico!! <<");
    StartupDiagnostics.LogStartupError(ex);
}
finally
{
    Log.Information(">> Servicio finalizado y recursos liberados <<");
    Log.CloseAndFlush();
}

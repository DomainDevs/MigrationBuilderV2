using App.Configurations;
using Application;
using Infrastructure;
using Infrastructure.Common.Diagnostics;
using Shared.Options;
using Persistence;
using Serilog;
using Infrastructure.Documentation;

try
{
    WebApplicationBuilder builder 
        = WebApplication.CreateBuilder(args);

    Log.Information("Servicio iniciando" );

    // ---------------------------------------------------------------------
    // Configuration
    // ---------------------------------------------------------------------
    builder.AddConfigurations();
    builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

    builder.Services.Configure<MigrationOptions>(
        builder.Configuration.GetSection(MigrationOptions.SectionName));

    // ---------------------------------------------------------------------
    // Service Registration (Manual)
    // ---------------------------------------------------------------------
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddPersistence(builder.Configuration)
        .AddApplication();


    // ---------------------------------------------------------------------
    // Service Registration (Automatic)
    // ---------------------------------------------------------------------
    builder.Services.AddBootstrap(
        false,
        (typeof(Application.AssemblyReference).Assembly, "Application.Features", "Handlers"),
        (typeof(Persistence.AssemblyReference).Assembly, "Persistence", "Repositories"),
        (typeof(Persistence.AssemblyReference).Assembly, "Persistence", "Services")
    );

    builder.Services.AddControllers();

    // ---------------------------------------------------------------------
    // Build
    // ---------------------------------------------------------------------
    var app = builder.Build();

    // ---------------------------------------------------------------------
    // Middleware / Startup
    // ---------------------------------------------------------------------
    app.UseInfrastructure(builder.Configuration);

    // Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseOpenApiDocumentation(builder.Configuration);
    }
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

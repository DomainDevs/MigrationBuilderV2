using App.Configurations;
using Application;
using Application.Features.Auth.DTOs;
using DataToolkit.Authentication.Extensions;
using DataToolkit.Bootstrap.Diagnostics;
using FluentValidation.Internal;
using Infrastructure;
using Infrastructure.Documentation;
using Persistence;
using Serilog;
using Shared.Options;
using System.Security.Claims;

try
{
    WebApplicationBuilder builder 
        = WebApplication.CreateBuilder(args);

    int hilosLogicos = Environment.ProcessorCount;
    Log.Information("Servicio iniciando" );

    // ---------------------------------------------------------------------
    // Configuration
    // ---------------------------------------------------------------------
    builder
        .AddConfigurations()
        .Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

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
        (typeof(Application.AssemblyReference).Assembly, "Application.Features", "Services"),
        (typeof(Persistence.AssemblyReference).Assembly, "Persistence", "Repositories"),
        (typeof(Persistence.AssemblyReference).Assembly, "Persistence", "Services")
    );

    // 1. Registras el servicio indicando cómo leer tu entidad de usuario
    builder.Services.AddDataToolkitAuthentication<ApplicationUser>()
    .MapUser(user =>
    {
        user.UserId = u => u.Id;
        user.Claims = u => new[] {
            new Claim(
                ClaimTypes.Role,u.Role)
        };
    })
    .AddJwt(jwt =>
    {
        jwt.SecretKey = builder.Configuration["Jwt:SecretKey"]!;
        
        jwt.Issuer = builder.Configuration["Jwt:Issuer"]!;
        
        jwt.Audience = builder.Configuration["Jwt:Audience"]!;

        jwt.AccessTokenLifetimeMinutes =
            int.Parse(builder.Configuration["Jwt:AccessTokenLifetimeMinutes"]!);

        jwt.RefreshTokenLifetimeHours =
            int.Parse(builder.Configuration["Jwt:RefreshTokenLifetimeHours"]!);
    });

    builder.Services.AddControllers();


    // ---------------------------------------------------------------------
    // Build
    // ---------------------------------------------------------------------
    var app = builder.Build();

    // ---------------------------------------------------------------------
    // Middleware / Startup
    // ---------------------------------------------------------------------
    app.UseInfrastructure(builder.Configuration);

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
    BootstrapDiagnostics.Report(ex);
    //StartupDiagnostics.LogStartupError(ex);
}
finally
{
    Log.Information(">> Servicio finalizado y recursos liberados <<");
    Log.CloseAndFlush();
}

using DataToolkit.Authentication.Abstractions;
using DataToolkit.Authentication.Cookies;
using DataToolkit.Authentication.Handlers;
using DataToolkit.Authentication.Jwt;
using DataToolkit.Authentication.Models;
using DataToolkit.Authentication.Refresh;
using DataToolkit.Authentication.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DataToolkit.Authentication.Builders;

public sealed class AuthenticationBuilder<TUser>
{
    private readonly IServiceCollection _services;
    private readonly AuthenticationOptions<TUser> _options = new();

    internal AuthenticationBuilder(
        IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configura el mapeo del usuario.
    /// </summary>
    public AuthenticationBuilder<TUser> MapUser(
        Action<AuthenticationOptions<TUser>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_options);

        return this;
    }

    /// <summary>
    /// Configura autenticación mediante JWT.
    /// </summary>
    public AuthenticationBuilder<TUser> AddJwt(
        Action<JwtOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        JwtOptions options = new();

        configure(options);

        options.Validate();

        UserMapping<TUser> mapping = _options.Build();

        SymmetricSecurityKey signingKey =
            KeyFactory.Create(options.SecretKey);

        _services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                authentication.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,

                        ValidateIssuer = true,
                        ValidIssuer = options.Issuer,

                        ValidateAudience = true,
                        ValidAudience = options.Audience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated =
                        JwtTokenValidatedEvent.Execute
                };
            });

        _services.AddSingleton(mapping);

        _services.AddSingleton<TokenEngine<TUser>>(_ =>
            new TokenEngine<TUser>(
                signingKey,
                options.Issuer,
                options.Audience,
                mapping,
                options.AccessTokenLifetimeMinutes));

        _services.AddSingleton<RefreshTokenEngine>(_ =>
            new RefreshTokenEngine(
                TimeSpan.FromHours(
                    options.RefreshTokenLifetimeHours)));

        _services.AddSingleton<IRefreshTokenStore,
            MemoryRefreshTokenStore>();

        _services.AddScoped<IAuthenticationHandler<TUser>,
            JwtHandler<TUser>>();

        _services.AddScoped<IAuthenticationService<TUser>,
            AuthenticationService<TUser>>();

        return this;
    }

    /// <summary>
    /// Configura autenticación mediante Cookies.
    /// </summary>
    public AuthenticationBuilder<TUser> AddCookies(
        Action<AuthenticationCookieOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        AuthenticationCookieOptions options = new();

        configure(options);

        options.Validate();

        return this;
    }
}
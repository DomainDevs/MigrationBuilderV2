using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DataToolkit.Authentication.Cookies;

/// <summary>
/// Gestiona la autenticación mediante Cookies.
/// </summary>
public sealed class CookieEngine
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationCookieOptions _options;

    public CookieEngine(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationCookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(options);

        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <summary>
    /// Inicia una sesión mediante Cookies.
    /// </summary>
    public async Task SignInAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "No active HttpContext was found.");

        var properties = new AuthenticationProperties
        {
            IsPersistent = _options.IsPersistent,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(_options.ExpireTimeSpan)
        };

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    /// <summary>
    /// Cierra la sesión actual.
    /// </summary>
    public async Task SignOutAsync()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "No active HttpContext was found.");

        await context.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
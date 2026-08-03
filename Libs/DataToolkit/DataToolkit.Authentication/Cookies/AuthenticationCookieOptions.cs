using Microsoft.AspNetCore.Http;

namespace DataToolkit.Authentication.Cookies;

/// <summary>
/// Configuración para autenticación mediante Cookies.
/// </summary>
public sealed class AuthenticationCookieOptions
{
    /// <summary>
    /// Nombre de la cookie de autenticación.
    /// </summary>
    public string CookieName { get; set; } = ".DataToolkit.Authentication";

    /// <summary>
    /// Ruta de inicio de sesión.
    /// </summary>
    public string LoginPath { get; set; } = "/login";

    /// <summary>
    /// Ruta de cierre de sesión.
    /// </summary>
    public string LogoutPath { get; set; } = "/logout";

    /// <summary>
    /// Tiempo de vida de la cookie.
    /// </summary>
    public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Renueva automáticamente la expiración de la cookie mientras el usuario permanezca activo.
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Política de envío de la cookie mediante HTTPS.
    /// </summary>
    public CookieSecurePolicy SecurePolicy { get; set; }
        = CookieSecurePolicy.Always;

    /// <summary>
    /// Configuración SameSite de la cookie.
    /// </summary>
    public SameSiteMode SameSite { get; set; }
        = SameSiteMode.Lax;

    /// <summary>
    /// Impide el acceso a la cookie desde JavaScript.
    /// </summary>
    public bool HttpOnly { get; set; } = true;

    /// <summary>
    /// Indica si la cookie debe persistir entre sesiones del navegador.
    /// </summary>
    public bool IsPersistent { get; set; } = true;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(CookieName))
            throw new InvalidOperationException(
                "CookieName is required.");

        if (ExpireTimeSpan <= TimeSpan.Zero)
            throw new InvalidOperationException(
                "ExpireTimeSpan must be greater than zero.");
    }
}
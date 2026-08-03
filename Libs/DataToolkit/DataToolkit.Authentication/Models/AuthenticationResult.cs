namespace DataToolkit.Authentication.Models;

public sealed class AuthenticationResult
{
    /// <summary>
    /// Access Token emitido por el proveedor.
    /// Solo aplica a esquemas basados en tokens.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh Token emitido por el proveedor.
    /// Solo aplica a esquemas que soportan renovación.
    /// </summary>
    public RefreshToken? RefreshToken { get; init; }

    /// <summary>
    /// Tipo de token.
    /// Ejemplo: Bearer.
    /// </summary>
    public string? TokenType { get; init; } //= "Bearer";

    /// <summary>
    /// Tiempo de expiración del Access Token en segundos.
    /// </summary>
    public int? ExpiresIn { get; init; }
}

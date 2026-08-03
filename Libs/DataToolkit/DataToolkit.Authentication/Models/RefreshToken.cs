namespace DataToolkit.Authentication.Models;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Value { get; init; } = string.Empty;

    public DateTime Created { get; init; }

    public DateTime Expires { get; init; }

    /// <summary>
    /// Fecha en la que el token fue revocado.
    /// </summary>
    public DateTime? Revoked { get; set; }

    /// <summary>
    /// Indica si el token ha expirado.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= Expires;

    /// <summary>
    /// Indica si el token fue revocado.
    /// </summary>
    public bool IsRevoked => Revoked.HasValue;

    /// <summary>
    /// Indica si el token sigue siendo válido.
    /// </summary>
    public bool IsActive => !IsExpired && !IsRevoked;
}
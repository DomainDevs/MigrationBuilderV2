namespace DataToolkit.Authentication.Models;

public sealed class AccessTokenEntry
{
    /// <summary>
    /// JWT generado.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Identificador único del JWT (claim "jti").
    /// </summary>
    public Guid Jti { get; init; }

    /// <summary>
    /// Usuario propietario del token.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Fecha de creación.
    /// </summary>
    public DateTime Created { get; init; }

    /// <summary>
    /// Fecha de expiración.
    /// </summary>
    public DateTime Expires { get; init; }

    /// <summary>
    /// Fecha de revocación.
    /// </summary>
    public DateTime? Revoked { get; set; }

    /// <summary>
    /// Indica si el token está activo.
    /// </summary>
    public bool IsActive =>
        Revoked is null &&
        Expires > DateTime.UtcNow;
}
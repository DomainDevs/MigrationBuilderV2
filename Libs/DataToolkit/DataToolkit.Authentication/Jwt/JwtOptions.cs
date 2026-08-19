namespace DataToolkit.Authentication.Jwt;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "DataToolkit.Authentication";

    public string Audience { get; set; } = "Default";

    public int AccessTokenLifetimeMinutes { get; set; } = 15; // 15 min (estándar seguro)

    public int RefreshTokenLifetimeHours { get; set; } = 168; // 7 días (168 hrs)

    public string SecretKey { get; set; } = string.Empty;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException(
                "El parámetro SecretKey es requerido.");

        if (SecretKey.Length < 32)
            throw new InvalidOperationException(
                "La clave SecretKey debe tener al menos 32 caracteres (256 bits) para HMAC-SHA256.");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException(
                "El parámetro Issuer es requerido.");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                "El parámetro Audience es requerido.");

        if (AccessTokenLifetimeMinutes <= 0)
            throw new InvalidOperationException(
                "AccessTokenLifetimeMinutes debe ser mayor que cero.");

        if (RefreshTokenLifetimeHours <= 0)
            throw new InvalidOperationException(
                "RefreshTokenLifetimeHours debe ser mayor que cero.");

        if (TimeSpan.FromHours(RefreshTokenLifetimeHours) <=
            TimeSpan.FromMinutes(AccessTokenLifetimeMinutes))
        {
            throw new InvalidOperationException(
                "La duración del token de actualización debe ser mayor que la duración del token de acceso.");
        }
    }
}
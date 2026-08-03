namespace DataToolkit.Authentication.Jwt;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "DataToolkit.Authentication";

    public string Audience { get; set; } = "Default";

    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    public int RefreshTokenLifetimeHours { get; set; } = 1;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException(
                "Issuer is required.");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                "Audience is required.");

        if (AccessTokenLifetimeMinutes <= 0)
            throw new InvalidOperationException(
                "AccessTokenLifetimeMinutes must be greater than zero.");

        if (RefreshTokenLifetimeHours <= 0)
            throw new InvalidOperationException(
                "RefreshTokenLifetimeHours must be greater than zero.");

        if (TimeSpan.FromDays(RefreshTokenLifetimeHours) <=
            TimeSpan.FromMinutes(AccessTokenLifetimeMinutes))
        {
            throw new InvalidOperationException(
                "Refresh token lifetime must be greater than access token lifetime.");
        }
    }
}

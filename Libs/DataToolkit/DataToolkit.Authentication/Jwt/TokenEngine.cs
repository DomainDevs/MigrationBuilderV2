using DataToolkit.Authentication.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DataToolkit.Authentication.Jwt;

public sealed class TokenEngine<TUser>
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    private readonly SymmetricSecurityKey _signingKey;
    private readonly UserMapping<TUser> _mapping;

    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenLifetimeMinutes;

    /// <summary>
    /// Tiempo de vida del Access Token, en minutos.
    /// </summary>
    public int AccessTokenLifetimeMinutes => _accessTokenLifetimeMinutes;

    public TokenEngine(
        SymmetricSecurityKey signingKey,
        string issuer,
        string audience,
        UserMapping<TUser> mapping,
        int accessTokenLifetimeMinutes = 60)
    {
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentNullException.ThrowIfNull(mapping);

        if (accessTokenLifetimeMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accessTokenLifetimeMinutes),
                "Access token lifetime must be greater than zero.");
        }

        _signingKey = signingKey;
        _issuer = issuer;
        _audience = audience;
        _mapping = mapping;
        _accessTokenLifetimeMinutes = accessTokenLifetimeMinutes;
    }

    /// <summary>
    /// Obtiene el identificador del usuario.
    /// </summary>
    public string GetUserId(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userId = _mapping.UserId(user);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "The user identifier cannot be null or empty.");
        }

        return userId;
    }

    /// <summary>
    /// Crea un nuevo Access Token.
    /// </summary>
    public AccessTokenEntry CreateToken(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        string userId = GetUserId(user);

        Guid jti = Guid.NewGuid();

        DateTime created = DateTime.UtcNow;

        DateTime expires = created.AddMinutes(_accessTokenLifetimeMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, jti.ToString())
        ];

        claims.AddRange(_mapping.Claims(user));

        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                _signingKey,
                SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = _tokenHandler.CreateToken(descriptor);

        return new AccessTokenEntry
        {
            Value = _tokenHandler.WriteToken(token),
            Jti = jti,
            UserId = userId,
            Created = created,
            Expires = expires
        };
    }

    /// <summary>
    /// Valida un Access Token.
    /// </summary>
    public bool ValidateToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            _tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,

                    ValidateIssuer = true,
                    ValidIssuer = _issuer,

                    ValidateAudience = true,
                    ValidAudience = _audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out _);

            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene los Claims contenidos en un Access Token.
    /// </summary>
    public IReadOnlyCollection<Claim> ReadClaims(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!_tokenHandler.CanReadToken(token))
        {
            return Array.Empty<Claim>();
        }

        return _tokenHandler
            .ReadJwtToken(token)
            .Claims
            .ToArray();
    }

    /// <summary>
    /// Obtiene el identificador (JTI) contenido en un Access Token.
    /// </summary>
    public Guid? GetTokenId(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!_tokenHandler.CanReadToken(token))
            return null;

        JwtSecurityToken jwtToken = _tokenHandler.ReadJwtToken(token);

        string? value = jwtToken.Claims
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)
            ?.Value;

        return Guid.TryParse(value, out Guid jti)
            ? jti
            : null;
    }

    /// <summary>
    /// Obtiene el identificador del usuario contenido en un Access Token.
    /// </summary>
    public string? GetTokenUserId(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!_tokenHandler.CanReadToken(token))
            return null;

        JwtSecurityToken jwtToken = _tokenHandler.ReadJwtToken(token);

        return jwtToken.Claims
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)
            ?.Value;
    }

}
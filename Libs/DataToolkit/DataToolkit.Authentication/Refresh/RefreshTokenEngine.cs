using System.Security.Cryptography;
using DataToolkit.Authentication.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace DataToolkit.Authentication.Refresh;
public sealed class RefreshTokenEngine
{
    private readonly TimeSpan _lifetime;

    public RefreshTokenEngine(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "Refresh token lifetime must be greater than zero.");

        _lifetime = lifetime;
    }

    public RefreshToken Generate()
    {
        Span<byte> bytes = stackalloc byte[32];

        RandomNumberGenerator.Fill(bytes);

        var now = DateTime.UtcNow;

        return new RefreshToken
        {
            Value = WebEncoders.Base64UrlEncode(bytes),
            Created = now,
            Expires = now + _lifetime
        };
    }
}
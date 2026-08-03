using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DataToolkit.Authentication.Refresh;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Authentication.Jwt;

internal static class JwtTokenValidatedEvent
{
    public static async Task Execute(TokenValidatedContext context)
    {
        IRefreshTokenStore store =
            context.HttpContext.RequestServices
                .GetRequiredService<IRefreshTokenStore>();

        Claim? claim = context.Principal?
            .FindFirst(JwtRegisteredClaimNames.Jti);

        if (claim is null)
        {
            Debug("JWT: Missing JTI.");

            context.Fail("Missing JTI.");

            return;
        }

        if (!Guid.TryParse(claim.Value, out Guid tokenId))
        {
            Debug($"JWT: Invalid JTI '{claim.Value}'.");

            context.Fail("Invalid JTI.");

            return;
        }

        Debug($"JWT: Validated. JTI = {tokenId}");

        if (await store.IsAccessTokenRevokedAsync(tokenId))
        {
            Debug($"JWT: Revoked. JTI = {tokenId}");

            context.Fail("Token revoked.");

            return;
        }

        Debug($"JWT: Accepted. JTI = {tokenId}");
    }

    [Conditional("DEBUG")]
    private static void Debug(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"[DataToolkit.Authentication] {message}");
        Console.ResetColor();
    }
}
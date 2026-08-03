using DataToolkit.Authentication.Abstractions;
using DataToolkit.Authentication.Jwt;
using DataToolkit.Authentication.Models;
using DataToolkit.Authentication.Refresh;

namespace DataToolkit.Authentication.Handlers;

internal sealed class JwtHandler<TUser> : IAuthenticationHandler<TUser>
{
    private readonly TokenEngine<TUser> _tokenEngine;
    private readonly RefreshTokenEngine _refreshTokenEngine;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IUserResolver<TUser> _userResolver;

    public JwtHandler(
        TokenEngine<TUser> tokenEngine,
        RefreshTokenEngine refreshTokenEngine,
        IRefreshTokenStore refreshTokenStore,
        IUserResolver<TUser> userResolver)
    {
        _tokenEngine = tokenEngine;
        _refreshTokenEngine = refreshTokenEngine;
        _refreshTokenStore = refreshTokenStore;
        _userResolver = userResolver;
    }

    public async Task<AuthenticationResult> SignInAsync(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        AccessTokenEntry accessToken =
            _tokenEngine.CreateToken(user);

        RefreshToken refreshToken =
            _refreshTokenEngine.Generate();

        await _refreshTokenStore.SaveAccessTokenAsync(accessToken);

        await _refreshTokenStore.SaveAsync(
            accessToken.UserId,
            refreshToken);

        return new AuthenticationResult
        {
            AccessToken = accessToken.Value,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = _tokenEngine.AccessTokenLifetimeMinutes * 60
        };
    }

    public async Task SignOutAsync(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        RefreshTokenEntry? entry =
            await _refreshTokenStore.FindAsync(refreshToken);

        if (entry is null)
        {
            return;
        }

        await _refreshTokenStore.RevokeAllAsync(entry.UserId);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        RefreshTokenEntry? entry =
            await _refreshTokenStore.FindAsync(refreshToken);

        if (entry is null)
        {
            throw new InvalidOperationException(
                "Refresh token not found.");
        }

        if (!entry.Token.IsActive)
        {
            throw new InvalidOperationException(
                "Refresh token expired or revoked.");
        }

        TUser? user =
            await _userResolver.FindAsync(entry.UserId);

        if (user is null)
        {
            throw new InvalidOperationException(
                "User not found.");
        }

        AccessTokenEntry accessToken =
            _tokenEngine.CreateToken(user);

        RefreshToken newRefreshToken =
            _refreshTokenEngine.Generate();

        await _refreshTokenStore.RevokeAsync(
            refreshToken);

        await _refreshTokenStore.SaveAccessTokenAsync(
            accessToken);

        await _refreshTokenStore.SaveAsync(
            accessToken.UserId,
            newRefreshToken);

        return new AuthenticationResult
        {
            AccessToken = accessToken.Value,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = _tokenEngine.AccessTokenLifetimeMinutes * 60
        };
    }
}
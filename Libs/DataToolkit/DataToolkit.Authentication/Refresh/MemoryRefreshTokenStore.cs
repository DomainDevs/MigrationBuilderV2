using System.Collections.Concurrent;
using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication.Refresh;

/// <summary>
/// Almacenamiento en memoria para Access Tokens y Refresh Tokens.
/// </summary>
public sealed class MemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<Guid, AccessTokenEntry> _accessTokens = new();

    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _refreshTokens = new();

    #region Refresh Tokens

    public Task SaveAsync(
        string userId,
        RefreshToken refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(refreshToken);

        _refreshTokens[refreshToken.Value] = new RefreshTokenEntry
        {
            UserId = userId,
            Token = refreshToken
        };

        return Task.CompletedTask;
    }

    public Task<RefreshTokenEntry?> FindAsync(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (_refreshTokens.TryGetValue(token, out var entry))
        {
            return Task.FromResult<RefreshTokenEntry?>(entry);
        }

        return Task.FromResult<RefreshTokenEntry?>(null);
    }

    public Task RevokeAsync(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (_refreshTokens.TryGetValue(token, out var entry))
        {
            entry.Token.Revoked = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Access Tokens

    public Task SaveAccessTokenAsync(AccessTokenEntry token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _accessTokens[token.Jti] = token;

        return Task.CompletedTask;
    }

    public Task<bool> IsAccessTokenRevokedAsync(Guid jti)
    {
        if (!_accessTokens.TryGetValue(jti, out var token))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(!token.IsActive);
    }

    public Task RevokeAccessTokenAsync(Guid jti)
    {
        if (_accessTokens.TryGetValue(jti, out var token))
        {
            token.Revoked = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    #endregion

    public Task RevokeAllAsync(string userId)
    {
        foreach (var token in _accessTokens.Values)
        {
            if (token.UserId == userId)
            {
                token.Revoked = DateTime.UtcNow;
            }
        }

        foreach (var token in _refreshTokens.Values)
        {
            if (token.UserId == userId)
            {
                token.Token.Revoked = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveExpiredAsync()
    {
        foreach (var pair in _accessTokens)
        {
            if (!pair.Value.IsActive)
            {
                _accessTokens.TryRemove(pair.Key, out _);
            }
        }

        foreach (var pair in _refreshTokens)
        {
            if (!pair.Value.Token.IsActive)
            {
                _refreshTokens.TryRemove(pair.Key, out _);
            }
        }

        return Task.CompletedTask;
    }
}
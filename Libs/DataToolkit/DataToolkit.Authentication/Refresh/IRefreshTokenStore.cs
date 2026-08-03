using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication.Refresh;

public interface IRefreshTokenStore
{
    #region Refresh Tokens

    Task SaveAsync(
        string userId,
        RefreshToken refreshToken);

    Task<RefreshTokenEntry?> FindAsync(string token);

    Task RevokeAsync(string token);

    #endregion

    #region Access Tokens

    Task SaveAccessTokenAsync(
        AccessTokenEntry accessToken);

    Task<bool> IsAccessTokenRevokedAsync(
        Guid jti);

    Task RevokeAccessTokenAsync(
        Guid jti);

    #endregion

    #region General

    Task RevokeAllAsync(
        string userId);

    Task RemoveExpiredAsync();

    #endregion
}
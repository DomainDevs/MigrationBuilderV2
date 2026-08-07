namespace Application.Features.Auth.DTOs;

public enum AuthenticationStatus
{
    Success,
    InvalidCredentials,
    InvalidRefreshToken,
    RefreshTokenExpired,
    RefreshTokenRevoked,
    UserNotFound
}

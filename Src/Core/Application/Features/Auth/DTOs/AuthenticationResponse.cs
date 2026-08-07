namespace Application.Features.Auth.DTOs;

public sealed class AuthenticationResponse
{
    public AuthenticationStatus Status { get; init; }

    public string? AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public string? TokenType { get; init; }

    public int ExpiresIn { get; init; }

    public string? Message { get; init; }
}

namespace Application.Features.Auth.DTOs;

public sealed class LogoutRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
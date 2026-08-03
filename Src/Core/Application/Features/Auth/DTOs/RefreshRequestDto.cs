namespace Application.Features.Auth.DTOs;

public sealed class RefreshRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

namespace Application.Features.Auth.DTOs;

//Diferente
public sealed class LoginResponse
{
    public string UserId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

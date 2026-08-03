namespace Application.Features.Auth.DTOs;


// ==========================================
// Clases de soporte locales de la aplicación
// ==========================================
public sealed class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
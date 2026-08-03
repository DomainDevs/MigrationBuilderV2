using Application.Features.Auth.DTOs;

namespace Application.Features.Auth.Services;

public sealed class AuthService : IAuthService
{
    private static readonly User[] Users =
    [
        new("1", "admin", "123456", "Administrador", "Admin"),
        new("2", "juan", "abc123", "Juan Pérez", "User")
    ];

    public Task<LoginResponse?> LoginAsync(string userName, string password)
    {
        foreach (var user in Users)
        {
            if (!user.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (user.Password != password)
                return Task.FromResult<LoginResponse?>(null);

            return Task.FromResult<LoginResponse?>(new LoginResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role
            });
        }

        return Task.FromResult<LoginResponse?>(null);
    }

    private sealed record User(
        string Id,
        string UserName,
        string Password,
        string FullName,
        string Role);
}
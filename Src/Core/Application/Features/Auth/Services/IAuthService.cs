namespace Application.Features.Auth.Services;

using Application.Features.Auth.DTOs;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string userName,string password);
}

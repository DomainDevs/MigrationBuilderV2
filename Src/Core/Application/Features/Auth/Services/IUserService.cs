using Application.Features.Auth.DTOs;

namespace Application.Features.Auth.Services
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task<ApplicationUser?> ValidateAsync(string userName, string password);
    }
}
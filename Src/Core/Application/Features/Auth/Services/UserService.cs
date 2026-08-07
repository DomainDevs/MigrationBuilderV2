using Application.Features.Auth.DTOs;

namespace Application.Features.Auth.Services;

public sealed class UserService : IUserService
{
    private static readonly List<ApplicationUser> Users =
    [
        new ApplicationUser
        {
            Id = "usr_987654",
            UserName = "Admin",
            Password = "123456",
            Role = "Administrator"
        }
    ];

    public Task<ApplicationUser?> ValidateAsync(
        string userName,
        string password)
    {
        ApplicationUser? user =
            Users.FirstOrDefault(x =>
                x.UserName == userName &&
                x.Password == password);

        return Task.FromResult(user);
    }

    public Task<ApplicationUser?> GetByIdAsync(
        string userId)
    {
        ApplicationUser? user =
            Users.FirstOrDefault(x =>
                x.Id == userId);

        return Task.FromResult(user);
    }
}

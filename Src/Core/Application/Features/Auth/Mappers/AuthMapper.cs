using Application.Features.Auth.Commands;
using Domain.DTOs.Auth;

namespace Application.Features.Auth.Mappers;
public static class ApplicationUserMapper
{
    public static ApplicationUser ToApplicationUser(
        this LoginCommand command)
    {
        return new ApplicationUser
        {
            UserName = command.UserName,
            PasswordHash = command.Password
        };
    }
}
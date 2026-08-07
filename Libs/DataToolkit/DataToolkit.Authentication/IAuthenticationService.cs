using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication
{
    public interface IAuthenticationService<TUser>
    {
        Task<string?> GetUserIdAsync(string refreshToken);
        Task<AuthenticationResult> RefreshAsync(TUser user, string refreshToken);
        Task<AuthenticationResult> SignInAsync(TUser user);
        Task SignOutAsync(string refreshToken);
    }
}
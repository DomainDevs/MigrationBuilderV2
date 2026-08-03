using DataToolkit.Authentication.Abstractions;
using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication.Handlers;

internal sealed class GoogleHandler<TUser>
    : IAuthenticationHandler<TUser>
{
    public Task<AuthenticationResult> SignInAsync(TUser user)
    {
        throw new NotImplementedException();
    }

    public Task SignOutAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticationResult> RefreshAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }
}
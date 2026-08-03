using DataToolkit.Authentication.Abstractions;
using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication;

/// <summary>
/// Servicio principal de autenticación.
/// </summary>
public sealed class AuthenticationService<TUser>
{
    private readonly IAuthenticationHandler<TUser> _handler;

    public AuthenticationService(
        IAuthenticationHandler<TUser> handler)
    {
        _handler = handler;
    }

    public Task<AuthenticationResult> SignInAsync(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return _handler.SignInAsync(user);
    }

    public Task SignOutAsync(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return _handler.SignOutAsync(refreshToken);
    }
    public Task<AuthenticationResult> RefreshAsync(
        string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return _handler.RefreshAsync(refreshToken);
    }

}
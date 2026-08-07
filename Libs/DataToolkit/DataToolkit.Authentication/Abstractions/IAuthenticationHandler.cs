using DataToolkit.Authentication.Models;

namespace DataToolkit.Authentication.Abstractions;

/// <summary>
/// Define el contrato para un proveedor de autenticación.
/// </summary>
/// <typeparam name="TUser">Tipo de usuario.</typeparam>
public interface IAuthenticationHandler<TUser>
{
    Task<AuthenticationResult> SignInAsync(
        TUser user);

    Task SignOutAsync(
        string refreshToken);

    Task<string?> GetUserIdAsync(
        string refreshToken);

    Task<AuthenticationResult> RefreshAsync(
        TUser user,
        string refreshToken);
}
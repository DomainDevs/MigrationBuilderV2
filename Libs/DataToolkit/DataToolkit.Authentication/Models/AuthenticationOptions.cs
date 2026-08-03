using System.Linq.Expressions;
using System.Security.Claims;

namespace DataToolkit.Authentication.Models;

/// <summary>
/// Configuración del mapeo del usuario utilizado por DataToolkit.Authentication.
/// </summary>
public sealed class AuthenticationOptions<TUser>
{
    /// <summary>
    /// Identificador único del usuario (JWT "sub").
    /// </summary>
    public Expression<Func<TUser, string>>? UserId { get; set; }

    /// <summary>
    /// Claims personalizados que se incluirán durante la autenticación.
    /// </summary>
    public Expression<Func<TUser, IEnumerable<Claim>>>? Claims { get; set; }

    internal UserMapping<TUser> Build()
    {
        Validate();

        return new UserMapping<TUser>
        {
            UserId = UserId!.Compile(),
            Claims = Claims?.Compile() ?? (_ => Enumerable.Empty<Claim>())
        };
    }

    private void Validate()
    {
        if (UserId is null)
            throw new InvalidOperationException(
                "UserId mapping is required.");
    }
}
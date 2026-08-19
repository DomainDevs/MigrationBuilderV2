using DataToolkit.Authentication.Abstractions;
using DataToolkit.Authentication.Builders;
using DataToolkit.Authentication.Security;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Authentication.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static AuthenticationBuilder<TUser> AddDataToolkitAuthentication<TUser>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return new AuthenticationBuilder<TUser>(services);
    }
}
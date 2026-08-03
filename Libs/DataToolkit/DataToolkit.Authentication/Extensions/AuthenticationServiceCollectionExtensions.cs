using DataToolkit.Authentication.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Authentication.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static AuthenticationBuilder<TUser> AddDataToolkitAuthentication<TUser>(
        this IServiceCollection services,
        string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        return new AuthenticationBuilder<TUser>(
            services,
            secretKey);
    }
}
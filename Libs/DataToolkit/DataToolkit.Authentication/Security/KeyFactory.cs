using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DataToolkit.Authentication.Security;

internal static class KeyFactory
{
    public static SymmetricSecurityKey Create(string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        return new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));
    }
}
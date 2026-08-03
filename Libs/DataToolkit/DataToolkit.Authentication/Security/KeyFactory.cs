using Microsoft.IdentityModel.Tokens;

namespace DataToolkit.Authentication.Security;

internal static class KeyFactory
{
    public static SymmetricSecurityKey Create(string secretKey)
    {
        byte[] signingKey = KeyDerivation.Derive(secretKey);

        return new SymmetricSecurityKey(signingKey);
    }
}
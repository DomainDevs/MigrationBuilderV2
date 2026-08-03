using System.Text;

namespace DataToolkit.Authentication.Security;

internal static class KeyDerivation
{
    public static byte[] Derive(string secretKey)
    {

        // TODO:
        // Replace with HKDF implementation.
        return Encoding.UTF8.GetBytes(secretKey);
    }
}

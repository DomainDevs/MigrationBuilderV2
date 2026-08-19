using DataToolkit.Authentication.Abstractions;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace DataToolkit.Authentication.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;
    private const int MaxIterationsAllowed = 5_000_000;
    private const int MaxPasswordLength = 1024;
    private const int StackAllocationLimit = 256;

    private const string Format = "PBKDF2";
    private const string Algorithm = "SHA256";

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        ValidatePasswordLength(password);

        Span<byte> salt = stackalloc byte[SaltSize];
        Span<byte> hash = stackalloc byte[HashSize];

        RandomNumberGenerator.Fill(salt);

        int passwordByteCount =
            Encoding.UTF8.GetByteCount(password);

        byte[]? rentedBuffer = null;

        Span<byte> passwordBytes =
            passwordByteCount <= StackAllocationLimit
                ? stackalloc byte[passwordByteCount]
                : (rentedBuffer =
                    ArrayPool<byte>.Shared.Rent(passwordByteCount));

        try
        {
            Encoding.UTF8.GetBytes(
                password,
                passwordBytes);

            Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                hash,
                Iterations,
                HashAlgorithmName.SHA256);

            return string.Join(
                '$',
                Format,
                Algorithm,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(hash);

            if (rentedBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(rentedBuffer);
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
            else
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);
        ValidatePasswordLength(password);

        ReadOnlySpan<char> hashSpan =
            passwordHash.AsSpan();

        Span<Range> ranges =
            stackalloc Range[5];

        int partCount =
            hashSpan.Split(ranges, '$');

        if (partCount != 5)
            return false;

        ReadOnlySpan<char> formatPart =
            hashSpan[ranges[0]];

        ReadOnlySpan<char> algorithmPart =
            hashSpan[ranges[1]];

        ReadOnlySpan<char> iterationsPart =
            hashSpan[ranges[2]];

        ReadOnlySpan<char> saltPart =
            hashSpan[ranges[3]];

        ReadOnlySpan<char> hashPart =
            hashSpan[ranges[4]];

        if (!formatPart.Equals(
                Format,
                StringComparison.Ordinal) ||
            !algorithmPart.Equals(
                Algorithm,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(
                iterationsPart,
                out int iterations) ||
            iterations <= 0 ||
            iterations > MaxIterationsAllowed)
        {
            return false;
        }

        Span<byte> salt =
            stackalloc byte[SaltSize];

        Span<byte> expectedHash =
            stackalloc byte[HashSize];

        Span<byte> actualHash =
            stackalloc byte[HashSize];

        if (!Convert.TryFromBase64Chars(
                saltPart,
                salt,
                out int saltBytesWritten) ||
            saltBytesWritten != SaltSize)
        {
            return false;
        }

        if (!Convert.TryFromBase64Chars(
                hashPart,
                expectedHash,
                out int hashBytesWritten) ||
            hashBytesWritten != HashSize)
        {
            CryptographicOperations.ZeroMemory(salt);
            return false;
        }

        int passwordByteCount =
            Encoding.UTF8.GetByteCount(password);

        byte[]? rentedBuffer = null;

        Span<byte> passwordBytes =
            passwordByteCount <= StackAllocationLimit
                ? stackalloc byte[passwordByteCount]
                : (rentedBuffer =
                    ArrayPool<byte>.Shared.Rent(passwordByteCount));

        try
        {
            Encoding.UTF8.GetBytes(
                password,
                passwordBytes);

            Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                actualHash,
                iterations,
                HashAlgorithmName.SHA256);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expectedHash);
            CryptographicOperations.ZeroMemory(actualHash);

            if (rentedBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(rentedBuffer);
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
            else
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }

    public bool NeedsRehash(string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);

        ReadOnlySpan<char> hashSpan =
            passwordHash.AsSpan();

        Span<Range> ranges =
            stackalloc Range[5];

        int partCount =
            hashSpan.Split(ranges, '$');

        if (partCount != 5)
            return true;

        ReadOnlySpan<char> formatPart =
            hashSpan[ranges[0]];

        ReadOnlySpan<char> algorithmPart =
            hashSpan[ranges[1]];

        ReadOnlySpan<char> iterationsPart =
            hashSpan[ranges[2]];

        if (!formatPart.Equals(
                Format,
                StringComparison.Ordinal) ||
            !algorithmPart.Equals(
                Algorithm,
                StringComparison.Ordinal))
        {
            return true;
        }

        if (!int.TryParse(
                iterationsPart,
                out int iterations))
        {
            return true;
        }

        return iterations < Iterations;
    }

    private static void ValidatePasswordLength(
        string password)
    {
        if (password.Length > MaxPasswordLength)
        {
            throw new ArgumentException(
                $"La contraseña no puede superar los {MaxPasswordLength} caracteres.",
                nameof(password));
        }
    }
}
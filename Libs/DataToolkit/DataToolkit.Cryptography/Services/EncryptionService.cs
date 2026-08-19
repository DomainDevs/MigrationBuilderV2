using DataToolkit.Cryptography.Abstractions;
using DataToolkit.Cryptography.Options;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace DataToolkit.Cryptography.Services;

internal sealed class EncryptionService : IEncryptionService, IDisposable
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int HeaderSize = NonceSize + TagSize;
    private const int StackAllocationLimit = 512;

    private readonly AesGcm _aesGcm;
    private bool _disposed;

    public EncryptionService(EncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        // Evitamos instanciar byte[] intermedios con UTF8.GetBytes al usar stackalloc/spans
        int keyByteCount = Encoding.UTF8.GetByteCount(options.SecretKey);
        Span<byte> secretKeyBytes = keyByteCount <= StackAllocationLimit
            ? stackalloc byte[keyByteCount]
            : new byte[keyByteCount];

        try
        {
            Encoding.UTF8.GetBytes(options.SecretKey, secretKeyBytes);

            Span<byte> key = stackalloc byte[32]; // SHA256 produce 256 bits (32 bytes)
            SHA256.HashData(secretKeyBytes, key);

            try
            {
                _aesGcm = new AesGcm(key, TagSize);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretKeyBytes);
        }
    }

    public string Encrypt(string plainText)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(plainText);

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(plainText.Length);
        byte[]? rentedBuffer = null;

        Span<byte> plainBytes = maxByteCount <= StackAllocationLimit
            ? stackalloc byte[maxByteCount]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount));

        try
        {
            int byteCount = Encoding.UTF8.GetBytes(plainText, plainBytes);
            ReadOnlySpan<byte> activePlainBytes = plainBytes[..byteCount];

            byte[] result = new byte[HeaderSize + byteCount];

            Span<byte> nonce = result.AsSpan(0, NonceSize);
            Span<byte> tag = result.AsSpan(NonceSize, TagSize);
            Span<byte> cipherBytes = result.AsSpan(HeaderSize);

            RandomNumberGenerator.Fill(nonce);

            _aesGcm.Encrypt(nonce, activePlainBytes, cipherBytes, tag);

            return Convert.ToBase64String(result);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                // Limpiamos el buffer completo del Pool, no solo el Slice asignado
                CryptographicOperations.ZeroMemory(rentedBuffer);
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
            else
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(cipherText);

        int bufferSize = cipherText.Length;
        byte[]? rentedBuffer = null;

        Span<byte> data = bufferSize <= StackAllocationLimit
            ? stackalloc byte[bufferSize]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize));

        try
        {
            if (!Convert.TryFromBase64String(cipherText, data, out int bytesWritten))
            {
                throw new CryptographicException("El texto cifrado no tiene un formato Base64 válido.");
            }

            ReadOnlySpan<byte> payload = data[..bytesWritten];

            if (payload.Length < HeaderSize)
            {
                throw new CryptographicException("El texto cifrado no tiene un tamaño válido.");
            }

            ReadOnlySpan<byte> nonce = payload[..NonceSize];
            ReadOnlySpan<byte> tag = payload[NonceSize..HeaderSize];
            ReadOnlySpan<byte> cipherBytes = payload[HeaderSize..];

            byte[] plainBytes = new byte[cipherBytes.Length];

            try
            {
                _aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("No fue posible descifrar el texto.", ex);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                // Limpiamos el buffer completo del Pool antes de devolverlo
                CryptographicOperations.ZeroMemory(rentedBuffer);
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
            else
            {
                CryptographicOperations.ZeroMemory(data);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _aesGcm.Dispose();
        _disposed = true;
    }
}
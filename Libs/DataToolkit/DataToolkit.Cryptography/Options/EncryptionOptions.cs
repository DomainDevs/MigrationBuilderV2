using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToolkit.Cryptography.Options;

public sealed class EncryptionOptions
{
    public string SecretKey { get; set; } = string.Empty;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException(
                "El parámetro SecretKey es requerido.");
    }
}
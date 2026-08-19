using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToolkit.Cryptography.Abstractions;

public interface IEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}

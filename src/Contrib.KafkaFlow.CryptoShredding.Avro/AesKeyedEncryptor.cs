using System.Security.Cryptography;
using System.Text;

namespace KafkaFlow.CryptoShredding.Avro;

public interface IKeyedEncryptor : IDisposable
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public sealed class AesKeyedEncryptor : IKeyedEncryptor
{
    private readonly Aes _aes;

    public AesKeyedEncryptor(string key)
    {
        _aes = Aes.Create();
        using var sha256 = SHA256.Create();
        _aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));

        var iv = new byte[16];
        Array.Copy(_aes.Key, iv, 16);
        _aes.IV = iv;
    }

    public string Encrypt(string plainText)
    {
        using var encryptor = _aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using var sw = new StreamWriter(cs);
            sw.Write(plainText);
        }
        var result = Convert.ToBase64String(ms.ToArray());
        return result;
    }

    public string Decrypt(string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);
        using var decryptor = _aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    public void Dispose()
    {
        _aes.Dispose();
    }
}

using KafkaFlow.CryptoShredding.Avro;

namespace KafkaFlow.CryptoShredding.Tests.Fixture;

public sealed class IdentityEncryptionKeyProvider : IEncryptionKeyProvider
{
    public Task<string> GetKey(string keyId) => Task.FromResult(keyId);
}

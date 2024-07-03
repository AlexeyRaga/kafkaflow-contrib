namespace KafkaFlow.CryptoShredding.Avro;

public interface IEncryptionKeyProvider
{
    Task<string> GetKey(string keyId);
}

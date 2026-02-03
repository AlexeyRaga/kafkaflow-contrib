using Avro.Util;
using FsCheck.Xunit;
using KafkaFlow.CryptoShredding.Avro;
using Avro.LogicalTypes;
using AwesomeAssertions;
using Contrib.KafkaFlow.CryptoShredding.TestContract;

namespace KafkaFlow.CryptoShredding.Tests;

[Properties(Arbitrary = [typeof(Generators)])]
public sealed class PlainEncryptedStringTests
{
    public PlainEncryptedStringTests()
    {
        LogicalTypeFactory.Instance.Register(new InlineEncryptedStringLogicalType());
        LogicalTypeFactory.Instance.Register(new EncryptedStringLogicalType());
    }

    [Property]
    public void Should_not_fail_decrypting(Guid key, Guid randomValue, InlineEncryptedMessage message)
    {
        using var encryptor = new AesKeyedEncryptor(key.ToString());
        AvroCryptoShredder.Encrypt(encryptor, message);

        message.email = randomValue.ToString();
        AvroCryptoShredder.Decrypt(encryptor, message);

        message.email.Should().Be(randomValue.ToString());
    }

    [Property]
    public void Should_encrypt_plain_strings(Guid key, InlineEncryptedMessage message)
    {
        using var encryptor = new AesKeyedEncryptor(key.ToString());

        var plainPayload = message.SerializeToJson();
        AvroCryptoShredder.Encrypt(encryptor, message);

        var encryptedPayload = message.SerializeToJson();
        encryptedPayload.Should().NotBeEquivalentTo(plainPayload);

        AvroCryptoShredder.Decrypt(encryptor, message);

        var decryptedPayload = message.SerializeToJson();
        decryptedPayload.Should().BeEquivalentTo(plainPayload);
    }


}

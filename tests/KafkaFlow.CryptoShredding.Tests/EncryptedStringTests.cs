using Avro;
using Avro.Util;
using Contrib.KafkaFlow.CryptoShredding.TestContract;
using FluentAssertions;
using FsCheck.Xunit;
using KafkaFlow.CryptoShredding.Avro;
using Avro.LogicalTypes;
using Newtonsoft.Json;

namespace KafkaFlow.CryptoShredding.Tests;

[Properties(Arbitrary = [typeof(Generators)])]
public class EncryptedStringTests
{
    public EncryptedStringTests()
    {
        LogicalTypeFactory.Instance.Register(new InlineEncryptedStringLogicalType());
        LogicalTypeFactory.Instance.Register(new EncryptedStringLogicalType());
    }

    [Property]
    public void Should_not_serialise_plain_secrets_to_binary(EncryptedMessage message)
    {
        var act = () => message.SerializeToBinary();
        act.Should().Throw<AvroTypeException>();
    }

    [Property]
    public void Should_roundtrip_message_to_json(Guid key, EncryptedMessage message)
    {
        using var encryptor = new AesKeyedEncryptor(key.ToString());
        var knownSecret = message.email;
        var secretText = ((EncryptedString.Decrypred)knownSecret).Value;

        AvroCryptoShredder.Encrypt(encryptor, message);

        var encryptedPayload = message.SerializeToJson();
        encryptedPayload.Should().NotContain(JsonConvert.ToString(secretText));

        var deserialisedMessage = AvroUtils.DeserializeFromJson<EncryptedMessage>(encryptedPayload);

        deserialisedMessage.Should().BeEquivalentTo(message);

        AvroCryptoShredder.Decrypt(encryptor, message);
        message.email.Should().BeEquivalentTo(knownSecret);
    }
}

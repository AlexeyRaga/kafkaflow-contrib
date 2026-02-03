using Contrib.KafkaFlow.CryptoShredding.TestContract;
using FsCheck.Xunit;
using KafkaFlow.CryptoShredding.Avro;
using KafkaFlow.CryptoShredding.Tests.Fixture;
using Avro.LogicalTypes;
using AwesomeAssertions;

namespace KafkaFlow.CryptoShredding.Tests;

public sealed class CryptoShreddingTests(CryptoShreddingFixture fixture): IClassFixture<CryptoShreddingFixture>
{
    [Property(MaxTest = 100)]
    public async Task Should_run_and_stop(string email, string apiKey)
    {
        var messageId = Guid.NewGuid();
        using var subscription = fixture.ReceivedMessages.Subscribe(x => x is UserRegistered foo && foo.id == messageId);

        var message = new UserRegistered
        {
            id = messageId,
            email = EncryptedString.FromPlain(email),
            apiKey = apiKey
        };

        var headers = new MessageHeaders();
        headers.SetString(CryptoShreddingHeaders.CryptoShreddingKey, message.id.ToString());

        await fixture.Producer.ProduceAsync(messageKey: message.id.ToString(), message, headers);
        await subscription.AwaitFor(TimeSpan.FromSeconds(10));

        var receivedMessage = subscription.Items.Last().Should().BeOfType<UserRegistered>().Subject;
        receivedMessage.apiKey.Should().BeEquivalentTo(apiKey);
        receivedMessage.email.Should().BeEquivalentTo(EncryptedString.FromPlain(email));
    }
}

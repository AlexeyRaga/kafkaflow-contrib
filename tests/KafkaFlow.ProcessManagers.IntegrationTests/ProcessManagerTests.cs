using FluentAssertions;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class ProcessManagerTests : IAssemblyFixture<KafkaFlowFixture>
{
    private readonly KafkaFlowFixture _fixture;

    public ProcessManagerTests(KafkaFlowFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    [Fact]
    public async Task Should_start_fixture()
    {
        var message = new UserRegistered(Guid.NewGuid(), "test@test.com");
        await _fixture.Producer.ProduceAsync(message.UserId.ToString(), message);

        await Task.Delay(TimeSpan.FromSeconds(10));

        _fixture.ProcessStateStore.Current.Should().BeEmpty();
        _fixture.ProcessStateStore
            .Changes
            .Select(x => x.Item1)
            .Should().BeEquivalentTo(new[]
            {
                LoggingProcessStateStore.ActionType.Persisted,
                LoggingProcessStateStore.ActionType.Persisted,
                LoggingProcessStateStore.ActionType.Deleted,
            }, x => x.WithStrictOrdering());
    }
}

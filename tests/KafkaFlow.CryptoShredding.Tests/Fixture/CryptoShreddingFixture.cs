using Avro.Specific;
using Contrib.KafkaFlow.CryptoShredding.TestContract;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.CryptoShredding.Tests.Fixture;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class CryptoShreddingFixture : KafkaFlowFixtureBase
{
    public ItemsObserver<ISpecificRecord> ReceivedMessages { get; } = new();
    protected override void ConfigureHandlers(TypedHandlerConfigurationBuilder builder) =>
        builder.AddHandler<CryptoShreddingHandlers>();

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddSingleton(ReceivedMessages);
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class CryptoShreddingHandlers(ItemsObserver<ISpecificRecord> observer) :
    IMessageHandler<UserRegistered>
{
    public Task Handle(IMessageContext context, UserRegistered message)
    {
        observer.Observe(message);
        return Task.CompletedTask;
    }
}

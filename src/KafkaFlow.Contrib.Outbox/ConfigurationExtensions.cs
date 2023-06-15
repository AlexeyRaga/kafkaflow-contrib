using KafkaFlow.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddOutboxBackend(this IServiceCollection services, Func<IServiceProvider, IOutboxBackend> factory) =>
        services.AddSingleton(factory);

    public static IServiceCollection AddOutboxBackend(this IServiceCollection services, IOutboxBackend backend) =>
        services.AddSingleton(backend);

    public static IProducerConfigurationBuilder WithOutbox(this IProducerConfigurationBuilder builder) =>
        builder.WithCustomFactory((producer, resolver) =>
            new OutboxProducerDecorator(producer, resolver.Resolve<IOutboxBackend>()));

    public static IProducerConfigurationBuilder WithOutbox(
        this IProducerConfigurationBuilder builder,
        IOutboxBackend outbox) =>
        builder.WithCustomFactory((producer, _) => new OutboxProducerDecorator(producer, outbox));
}

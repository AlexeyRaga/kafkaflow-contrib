using KafkaFlow.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox;

public interface IOutboxDispatcher {}

public static class ConfigurationExtensions
{
    public static IServiceCollection AddOutboxBackend(this IServiceCollection services, Func<IServiceProvider, IOutboxBackend> factory) =>
        services.AddSingleton(factory);

    public static IServiceCollection AddOutboxBackend(this IServiceCollection services, IOutboxBackend backend) =>
        services.AddSingleton(backend);

    public static IProducerConfigurationBuilder WithOutbox(this IProducerConfigurationBuilder builder)
    {
        return builder.WithCustomFactory((producer, resolver) =>
            new OutboxProducerDecorator(producer, resolver.Resolve<IOutboxBackend>()));
    }

    public static IProducerConfigurationBuilder WithOutbox(
        this IProducerConfigurationBuilder builder,
        IOutboxBackend outbox) =>
        builder.WithCustomFactory((producer, dec) => new OutboxProducerDecorator(producer, outbox));

    public static IClusterConfigurationBuilder AddOutboxDispatcher(
        this IClusterConfigurationBuilder builder,
        Action<IOutboxProducerConfigurationBuilder>? configure = null)
    {
        builder.AddProducer<IOutboxDispatcher>(p =>
        {
            var producerBuilder = new OutboxProducerConfigurationBuilder(p);
            configure?.Invoke(producerBuilder);
            producerBuilder.Build();
        });
        builder.DependencyConfigurator.AddSingleton<OutboxDispatcherService>();
        builder.OnStarted(r =>
        {
            var srv = r.Resolve<OutboxDispatcherService>();
            srv.StartAsync(default);
        });

        builder.OnStopping(r =>
        {
            var srv = r.Resolve<OutboxDispatcherService>();
            srv.StopAsync(default).GetAwaiter().GetResult();
        });
        return builder;
    }
}

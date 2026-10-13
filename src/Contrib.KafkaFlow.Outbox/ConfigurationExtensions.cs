using KafkaFlow.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.Outbox;

public static class ConfigurationExtensions
{
    private static readonly TimeSpan DispatcherStopTimeout = TimeSpan.FromSeconds(10);

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
        builder.DependencyConfigurator.AddSingleton(r => new OutboxDispatcherService(
            r.Resolve<ILogger<OutboxDispatcherService>>(),
            r.Resolve<IMessageProducer<IOutboxDispatcher>>(),
            r.Resolve<IOutboxBackend>(),
            TimeProvider.System));
        builder.OnStarted(r =>
        {
            var srv = r.Resolve<OutboxDispatcherService>();
            srv.StartAsync(default);
        });

        builder.OnStopping(r =>
        {
            var srv = r.Resolve<OutboxDispatcherService>();
            using var timeout = new CancellationTokenSource(DispatcherStopTimeout);
            srv.StopAsync(timeout.Token).GetAwaiter().GetResult();
        });
        return builder;
    }
}

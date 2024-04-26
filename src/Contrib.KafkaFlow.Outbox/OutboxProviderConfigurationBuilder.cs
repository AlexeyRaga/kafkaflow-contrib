using Confluent.Kafka;
using KafkaFlow.Configuration;

namespace KafkaFlow.Outbox;

internal sealed class OutboxProducerConfigurationBuilder(IProducerConfigurationBuilder builder) : IOutboxProducerConfigurationBuilder
{
    private readonly IProducerConfigurationBuilder _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    private readonly ProducerConfig _producerConfig = new();

    public IDependencyConfigurator DependencyConfigurator => _builder.DependencyConfigurator;

    public IOutboxProducerConfigurationBuilder WithPartitioner(Partitioner partitioner)
    {
        _producerConfig.Partitioner = partitioner;
        return this;
    }

    public IOutboxProducerConfigurationBuilder WithCompression(CompressionType compressionType, int? compressionLevel)
    {
        _producerConfig.CompressionType = compressionType;
        _producerConfig.CompressionLevel = compressionLevel;
        return this;
    }

    public IOutboxProducerConfigurationBuilder WithAcks(Acks acks) => WithBuilder(x => x.WithAcks(acks));

    public IOutboxProducerConfigurationBuilder WithLingerMs(double lingerMs) => WithBuilder(x => x.WithLingerMs(lingerMs));

    public IOutboxProducerConfigurationBuilder WithStatisticsHandler(Action<string> statisticsHandler) =>
        WithBuilder(x => x.WithStatisticsHandler(statisticsHandler));

    public IOutboxProducerConfigurationBuilder WithStatisticsIntervalMs(int statisticsIntervalMs) =>
        WithBuilder(x => x.WithStatisticsIntervalMs(statisticsIntervalMs));

    private OutboxProducerConfigurationBuilder WithBuilder(Action<IProducerConfigurationBuilder> action)
    {
        action(_builder);
        return this;
    }

    public IProducerConfigurationBuilder Build() =>
        _builder.WithProducerConfig(_producerConfig);
}

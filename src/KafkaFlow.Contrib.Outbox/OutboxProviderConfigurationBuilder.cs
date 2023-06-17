using Confluent.Kafka;
using KafkaFlow.Configuration;

namespace KafkaFlow.Outbox;

public interface IOutboxProducerConfigurationBuilder
{
    IDependencyConfigurator DependencyConfigurator { get; }

    /// <summary>
    /// Sets the <see cref="Acks"/> to be used when producing messages
    /// </summary>
    /// <param name="acks">The <see cref="Acks"/> enum value</param>
    /// <returns></returns>
    IOutboxProducerConfigurationBuilder WithAcks(Acks acks);

    /// <summary>
    /// Delay in milliseconds to wait for messages in the producer queue to accumulate before constructing message batches to transmit to brokers.
    /// A higher value allows larger and more effective (less overhead, improved compression) batches of messages to accumulate at the expense of increased message delivery latency.
    /// default: 0.5 (500 microseconds)
    /// importance: high
    /// </summary>
    /// <param name="lingerMs">The time in milliseconds to wait to build the message batch</param>
    /// <returns></returns>
    IOutboxProducerConfigurationBuilder WithLingerMs(double lingerMs);

    /// <summary>
    /// Adds a handler for the Kafka producer statistics
    /// </summary>
    /// <param name="statisticsHandler">A handler for the statistics</param>
    /// <returns></returns>
    IOutboxProducerConfigurationBuilder WithStatisticsHandler(Action<string> statisticsHandler);

    /// <summary>
    /// Sets the interval the statistics are emitted
    /// </summary>
    /// <param name="statisticsIntervalMs">The interval in miliseconds</param>
    /// <returns></returns>
    IOutboxProducerConfigurationBuilder WithStatisticsIntervalMs(int statisticsIntervalMs);

    /// <summary>
    /// Sets the partitioner
    /// </summary>
    /// <param name="partitioner">The partitioner to use</param>
    /// <returns></returns>
    IOutboxProducerConfigurationBuilder WithPartitioner(Partitioner partitioner);

    /// <summary>
    /// Sets compression configurations in the producer
    /// </summary>
    /// <param name="compressionType">
    /// <see cref="P:Confluent.Kafka.CompressionType"/> enum to select the compression codec to use for compressing message sets. This is the default value for all topics, may be overridden by the topic configuration property `compression.codec`.
    /// default: none
    /// importance: medium</param>
    /// <param name="compressionLevel">
    /// Compression level parameter for algorithm selected by <see cref="P:Confluent.Kafka.CompressionType"/> enum. Higher values will result in better compression at the cost of more CPU usage. Usable range is algorithm-dependent: [0-9] for gzip; [0-12] for lz4; only 0 for snappy; -1 = codec-dependent default compression level.
    /// default: -1
    /// importance: medium
    /// </param>
    /// <returns></returns>
    IOutboxProducerConfigurationBuilder WithCompression(CompressionType compressionType, int? compressionLevel);
}

internal sealed class OutboxProducerConfigurationBuilder : IOutboxProducerConfigurationBuilder
{
    private readonly IProducerConfigurationBuilder _builder;
    private readonly ProducerConfig _producerConfig = new();

    public OutboxProducerConfigurationBuilder(IProducerConfigurationBuilder builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

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

    private IOutboxProducerConfigurationBuilder WithBuilder(Action<IProducerConfigurationBuilder> action)
    {
        action(_builder);
        return this;
    }

    public IProducerConfigurationBuilder Build() =>
        _builder.WithProducerConfig(_producerConfig);
}

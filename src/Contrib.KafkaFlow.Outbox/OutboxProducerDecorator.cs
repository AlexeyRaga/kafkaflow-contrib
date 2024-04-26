using Confluent.Kafka;

namespace KafkaFlow.Outbox;

internal sealed class OutboxProducerDecorator(IProducer<byte[], byte[]> inner, IOutboxBackend outbox) : IProducer<byte[], byte[]>
{
    private readonly IProducer<byte[], byte[]> _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly IOutboxBackend _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));

    public void Dispose() => _inner.Dispose();

    public int AddBrokers(string brokers) => _inner.AddBrokers(brokers);

    public Handle Handle => _inner.Handle;
    public string Name => _inner.Name;

    public Task<DeliveryResult<byte[], byte[]>> ProduceAsync(
        string topic,
        Message<byte[], byte[]> message,
        CancellationToken cancellationToken = default) =>
        ProduceAsync(new TopicPartition(topic, Partition.Any), message, cancellationToken);

    public async Task<DeliveryResult<byte[], byte[]>> ProduceAsync(
        TopicPartition topicPartition,
        Message<byte[], byte[]> message,
        CancellationToken cancellationToken = default)
    {
        await _outbox.Store(topicPartition, message, cancellationToken).ConfigureAwait(false);
        return new DeliveryResult<byte[], byte[]>
        {
            Topic = topicPartition.Topic,
            Partition = topicPartition.Partition,
            Offset = Offset.Unset,
            Message = message,
            Status = PersistenceStatus.NotPersisted
        };
    }

    public void Produce(string topic, Message<byte[], byte[]> message,
        Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null) => ProduceAsync(topic, message).ConfigureAwait(false).GetAwaiter().GetResult();

    public void Produce(TopicPartition topicPartition, Message<byte[], byte[]> message,
        Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null) => ProduceAsync(topicPartition, message).ConfigureAwait(false).GetAwaiter().GetResult();

    public void SetSaslCredentials(string username, string password)
    {
        // Do nothing, storing in the outbox does not require SASL
    }

    public int Poll(TimeSpan timeout) => _inner.Poll(timeout);

    public int Flush(TimeSpan timeout) => _inner.Flush(timeout);

    public void Flush(CancellationToken cancellationToken = default) => _inner.Flush(cancellationToken);

    public void InitTransactions(TimeSpan timeout) => throw new InvalidOperationException("This producer does not support transactions");

    public void BeginTransaction() => throw new InvalidOperationException("This producer does not support transactions");

    public void CommitTransaction(TimeSpan timeout) => throw new InvalidOperationException("This producer does not support transactions");

    public void CommitTransaction() => throw new InvalidOperationException("This producer does not support transactions");

    public void AbortTransaction(TimeSpan timeout) => throw new InvalidOperationException("This producer does not support transactions");

    public void AbortTransaction() => throw new InvalidOperationException("This producer does not support transactions");
    public void SendOffsetsToTransaction(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout) => throw new InvalidOperationException("This producer does not support transactions");
}

using Confluent.Kafka;

namespace KafkaFlow.Outbox.Postgres;

public class PostgresOutboxBackend : IOutboxBackend
{
    public PostgresOutboxBackend()
    {

    }

    public ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}

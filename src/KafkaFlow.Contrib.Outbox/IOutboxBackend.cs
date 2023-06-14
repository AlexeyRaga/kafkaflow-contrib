using Confluent.Kafka;

namespace KafkaFlow.Outbox;

public sealed record OutboxRecord(TopicPartition TopicPartition, Message<byte[], byte[]> Message);
public interface IOutboxBackend
{
    ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default);
    ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default);
}

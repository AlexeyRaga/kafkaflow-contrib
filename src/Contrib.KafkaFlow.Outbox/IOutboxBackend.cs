using Confluent.Kafka;

namespace KafkaFlow.Outbox;
public interface IOutboxBackend
{
    ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token2 = default);
    ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default);
}

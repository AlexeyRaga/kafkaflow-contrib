using Confluent.Kafka;

namespace KafkaFlow.Outbox;

public sealed record OutboxRecord(TopicPartition TopicPartition, Message<byte[], byte[]> Message);

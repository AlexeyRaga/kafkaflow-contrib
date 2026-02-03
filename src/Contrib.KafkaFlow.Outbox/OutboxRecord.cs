using Confluent.Kafka;

namespace KafkaFlow.Outbox;

public sealed record OutboxRecord(Confluent.Kafka.TopicPartition TopicPartition, Message<byte[], byte[]> Message);

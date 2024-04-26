namespace KafkaFlow.Outbox;

public sealed record OutboxTableRow(long SequenceId, string TopicName, int? Partition, byte[]? MessageKey, string? MessageHeaders, byte[]? MessageBody)
{
    public OutboxTableRow(string TopicName, int? Partition, byte[]? MessageKey, string? MessageHeaders, byte[]? MessageBody)
        : this(0, TopicName, Partition, MessageKey, MessageHeaders, MessageBody) { }
}

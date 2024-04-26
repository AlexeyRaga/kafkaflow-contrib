using Confluent.Kafka;
using System.Text.Json;

namespace KafkaFlow.Outbox;

public class OutboxBackend(IOutboxRepository outboxRepository) : IOutboxBackend
{
    private readonly IOutboxRepository _outboxRepository = outboxRepository;

    public async ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default)
    {
        var rawHeaders =
            message.Headers == null
            ? null
            : JsonSerializer.Serialize(message.Headers.ToDictionary(x => x.Key, x => x.GetValueBytes()));

        await _outboxRepository.Store(
            new OutboxTableRow
            (
                topicPartition.Topic,
                topicPartition.Partition.IsSpecial ? null : topicPartition.Partition.Value,
                message.Key,
                rawHeaders,
                message.Value
            ), token
        ).ConfigureAwait(false);
    }

    public async ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default)
    {
        var result = await _outboxRepository.Read(batchSize, token).ConfigureAwait(false);
        return result?.Select(ToOutboxRecord).ToArray() ?? [];
    }

    private static OutboxRecord ToOutboxRecord(OutboxTableRow row)
    {
        var partition = row.Partition.HasValue ? new Partition(row.Partition.Value) : Partition.Any;
        var topicPartition = new TopicPartition(row.TopicName, partition);

        var storedHeaders =
            row.MessageHeaders == null
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, byte[]>>(row.MessageHeaders);

        var headers =
            storedHeaders?.Aggregate(new Headers(), (h, x) =>
            {
                h.Add(x.Key, x.Value);
                return h;
            });

        var msg = new Message<byte[], byte[]>
        {
            Key = row.MessageKey!,
            Value = row.MessageBody!,
            Headers = headers
        };

        return new OutboxRecord(topicPartition, msg);
    }
}

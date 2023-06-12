using System.Collections.Concurrent;
using Confluent.Kafka;

namespace KafkaFlow.Outbox.InMemory;

public sealed class InMemoryOutboxBackend : IOutboxBackend
{
    private readonly ConcurrentQueue<(TopicPartition, Message<byte[], byte[]>)> _queue = new();

    public ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default)
    {
        _queue.Enqueue((topicPartition, message));
        return ValueTask.CompletedTask;
    }

    public ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default)
    {
        var batch =
            Enumerable
                .Range(0, batchSize)
                .Select(_ => _queue.TryDequeue(out var value) ? new OutboxRecord(value.Item1, value.Item2) : null)
                .TakeWhile(x => x != null)
                .Select(x => x!)
                .ToArray();

        return ValueTask.FromResult(batch);
    }

    public ValueTask Purge()
    {
        _queue.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<OutboxRecord[]> GetAll() => Read(int.MaxValue);
}

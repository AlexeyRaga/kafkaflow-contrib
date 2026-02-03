using Confluent.Kafka;
using System.Collections.Concurrent;

namespace KafkaFlow.Outbox.InMemory;

file sealed class NoOpTransactionScope : ITransactionScope
{
    public void Complete() { }
    public void Dispose() { }
}

public sealed class InMemoryOutboxBackend : IOutboxBackend
{
    private readonly ConcurrentQueue<(Confluent.Kafka.TopicPartition, Message<byte[], byte[]>)> _queue = new();

    public ITransactionScope BeginTransaction() => new NoOpTransactionScope();

    public ValueTask Store(Confluent.Kafka.TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default)
    {
        _queue.Enqueue((topicPartition, message));
        return default;
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

        return new ValueTask<OutboxRecord[]>(batch);
    }

    public ValueTask Purge()
    {
#if NETSTANDARD2_1_OR_GREATER
        _queue.Clear();                         // Unfortunately .NetStandard 2.1+ is required for this
#else
        while (_queue.TryDequeue(out _)) { }    // So we have to do it this way
#endif
        return default;
    }

    public ValueTask<OutboxRecord[]> GetAll() => Read(int.MaxValue);
}

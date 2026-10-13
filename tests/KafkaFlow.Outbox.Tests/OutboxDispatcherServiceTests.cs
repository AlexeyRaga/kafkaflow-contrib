using System.Transactions;
using AwesomeAssertions;
using Confluent.Kafka;
using KafkaFlow.Producers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace KafkaFlow.Outbox.Tests;

using TopicPartition = Confluent.Kafka.TopicPartition;

public sealed class OutboxDispatcherServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly FakeTimeProvider _time = new();
    private readonly Mock<IMessageProducer<IOutboxDispatcher>> _producer = new();
    private int _produced;

    [Fact]
    public async Task Batch_whose_transaction_aborts_is_retried()
    {
        var outbox = new FakeOutbox([Record()], [Record()]);
        OnProduce(FirstProduceAbortsTransaction);
        using var dispatcher = NewDispatcher(outbox);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntil(() => Volatile.Read(ref _produced) == 2, advanceBy: TimeSpan.FromSeconds(10));

        outbox.AbortedTransactions.Should().Be(1);
        dispatcher.ExecuteTask!.IsCompleted.Should().BeFalse();
        await dispatcher.StopAsync(CancellationToken.None);
    }

    private static OutboxRecord Record(Partition? partition = null) =>
        new(new TopicPartition("topic", partition ?? Partition.Any), new Message<byte[], byte[]>());

    // Same state as the transaction timeout firing mid-produce, without waiting for it.
    private Task FirstProduceAbortsTransaction()
    {
        if (Interlocked.Increment(ref _produced) == 1)
            Transaction.Current!.Rollback();
        return Task.CompletedTask;
    }

    private OutboxDispatcherService NewDispatcher(IOutboxBackend outbox) =>
        new(NullLogger<OutboxDispatcherService>.Instance, _producer.Object, outbox, _time);

    private void OnProduce(Func<Task> produce) =>
        _producer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<IMessageHeaders>(), It.IsAny<int?>()))
            .Returns(async () =>
            {
                await produce();
                return new DeliveryResult<byte[], byte[]>();
            });

    private async Task WaitUntil(Func<bool> condition, TimeSpan advanceBy = default)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        while (!condition())
        {
            _time.Advance(advanceBy);
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private sealed class FakeOutbox(params OutboxRecord[][] batches) : IOutboxBackend
    {
        private int _reads;
        private int _abortedTransactions;

        public int Reads => Volatile.Read(ref _reads);

        public int AbortedTransactions => Volatile.Read(ref _abortedTransactions);

        public ITransactionScope BeginTransaction() =>
            new ObservedScope(
                SystemTransactionScope.Create(),
                () => Interlocked.Increment(ref _abortedTransactions));

        public ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default) =>
            throw new NotSupportedException();

        public ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default)
        {
            var read = Interlocked.Increment(ref _reads);
            return ValueTask.FromResult(read <= batches.Length ? batches[read - 1] : []);
        }

        private sealed class ObservedScope(ITransactionScope scope, Action onAborted) : ITransactionScope
        {
            public void Complete() => scope.Complete();

            public void Dispose()
            {
                try
                {
                    scope.Dispose();
                }
                catch (TransactionAbortedException)
                {
                    onAborted();
                    throw;
                }
            }
        }
    }
}

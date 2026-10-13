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

    [Fact]
    public async Task Batch_whose_transaction_aborts_stops_producing_its_remaining_messages()
    {
        var outbox = new FakeOutbox([Record(), Record()], [Record(), Record()]);
        OnProduce(FirstProduceAbortsTransaction);
        using var dispatcher = NewDispatcher(outbox);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntil(() => outbox.Reads > 2, advanceBy: TimeSpan.FromSeconds(10));
        await dispatcher.StopAsync(CancellationToken.None);

        _produced.Should().Be(3, "the aborted batch stops after its first message; the retry sends both");
    }

    [Fact]
    public async Task Batch_whose_non_system_transaction_aborts_stops_producing_its_remaining_messages()
    {
        ManualScope? scope = null;
        var outbox = new FakeOutbox([Record(), Record()]) { NewScope = () => scope = new ManualScope() };
        OnProduce(() =>
        {
            Interlocked.Increment(ref _produced);
            scope!.IsActive = false;
            return Task.CompletedTask;
        });
        using var dispatcher = NewDispatcher(outbox);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntil(() => outbox.Reads > 1, advanceBy: TimeSpan.FromSeconds(10));
        await dispatcher.StopAsync(CancellationToken.None);

        _produced.Should().Be(1);
    }

    [Fact]
    public async Task Stop_does_not_wait_for_a_stuck_produce()
    {
        var stuck = new TaskCompletionSource();
        OnProduce(() =>
        {
            Interlocked.Increment(ref _produced);
            return stuck.Task;
        });
        using var dispatcher = NewDispatcher(new FakeOutbox([Record()]));

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntil(() => Volatile.Read(ref _produced) == 1);

        await dispatcher.StopAsync(CancellationToken.None).WaitAsync(Timeout);
    }

    [Fact]
    public async Task Stop_does_not_wait_for_a_blocked_read_and_ignores_rollback_errors()
    {
        var outbox = new Mock<IOutboxBackend>();
        var reading = new TaskCompletionSource();
        var scope = new Mock<ITransactionScope>();
        scope.Setup(t => t.Dispose()).Throws(new InvalidOperationException("rollback during a pending read"));
        outbox.Setup(o => o.BeginTransaction()).Returns(scope.Object);
        outbox.Setup(o => o.Read(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                reading.TrySetResult();
                return new ValueTask<OutboxRecord[]>(new TaskCompletionSource<OutboxRecord[]>().Task);
            });
        using var dispatcher = NewDispatcher(outbox.Object);

        await dispatcher.StartAsync(CancellationToken.None);
        await reading.Task.WaitAsync(Timeout);

        await dispatcher.StopAsync(CancellationToken.None).WaitAsync(Timeout);
        dispatcher.ExecuteTask!.IsFaulted.Should().BeFalse();
    }

    private static OutboxRecord Record() =>
        new(new TopicPartition("topic", Partition.Any), new Message<byte[], byte[]>());

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

        public Func<ITransactionScope> NewScope { get; init; } = () => SystemTransactionScope.Create();

        public ITransactionScope BeginTransaction() =>
            new ObservedScope(
                NewScope(),
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
            public bool IsActive => scope.IsActive;

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

    private sealed class ManualScope : ITransactionScope
    {
        public bool IsActive { get; set; } = true;

        public void Complete() { }

        public void Dispose() { }
    }
}

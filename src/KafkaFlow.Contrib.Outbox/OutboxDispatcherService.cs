using System.Transactions;
using Microsoft.Extensions.Hosting;

namespace KafkaFlow.Outbox;

internal sealed class OutboxDispatcherService : BackgroundService
{
    private readonly IMessageProducer<IOutboxDispatcher> _producer;
    private readonly IOutboxBackend _outboxBackend;

    public OutboxDispatcherService(IMessageProducer<IOutboxDispatcher> producer, IOutboxBackend outboxBackend)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _outboxBackend = outboxBackend ?? throw new ArgumentNullException(nameof(outboxBackend));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = BeginTransaction)
            {
                var batch = await _outboxBackend.Read(10, stoppingToken).ConfigureAwait(false);
                foreach (var record in batch)
                {
                    var headers = record.Message.Headers == null ? null : new MessageHeaders(record.Message.Headers);
                    await _producer.ProduceAsync(record.TopicPartition.Topic, record.Message.Key, record.Message.Value, headers);
                }
                scope.Complete();
            }

            Console.WriteLine("I am still there");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private static TransactionScope BeginTransaction =>
        new(
            scopeOption: TransactionScopeOption.RequiresNew,
            transactionOptions: new TransactionOptions
                { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);
}

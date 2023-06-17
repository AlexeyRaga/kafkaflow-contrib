using System.Transactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.Outbox;

internal sealed class OutboxDispatcherService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IMessageProducer<IOutboxDispatcher> _producer;
    private readonly IOutboxBackend _outboxBackend;

    public OutboxDispatcherService(
        ILogger<OutboxDispatcherService> logger,
        IMessageProducer<IOutboxDispatcher> producer,
        IOutboxBackend outboxBackend)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _outboxBackend = outboxBackend ?? throw new ArgumentNullException(nameof(outboxBackend));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher service has started");
        while (!stoppingToken.IsCancellationRequested)
        {
            var hadBatch = await DispatchNextBatchAsync(stoppingToken);
            if (!hadBatch)
            {
                _logger.LogDebug("The dispatcher queue is empty, will sleep before the next poll");
                // if there was nothing to dispatch, sleep for 1 second before the next check
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        _logger.LogInformation("Outbox dispatcher service has ыещззув");
    }

    private async Task<bool> DispatchNextBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = BeginTransaction;
        var batch = await _outboxBackend.Read(10, stoppingToken).ConfigureAwait(false);

        foreach (var record in batch)
        {
            var headers = record.Message.Headers == null ? null : new MessageHeaders(record.Message.Headers);
            await _producer.ProduceAsync(record.TopicPartition.Topic, record.Message.Key, record.Message.Value, headers);
        }

        scope.Complete();
        return batch.Any();
    }

    private static TransactionScope BeginTransaction =>
        new(
            scopeOption: TransactionScopeOption.RequiresNew,
            transactionOptions: new TransactionOptions
                { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Transactions;
using KafkaFlow.Producers;

namespace KafkaFlow.Outbox;

internal sealed class OutboxDispatcherService(
    ILogger<OutboxDispatcherService> logger,
    IMessageProducer<IOutboxDispatcher> producer,
    IOutboxBackend outboxBackend) : BackgroundService
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMessageProducer<IOutboxDispatcher> _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    private readonly IOutboxBackend _outboxBackend = outboxBackend ?? throw new ArgumentNullException(nameof(outboxBackend));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher service has started");
        while (!stoppingToken.IsCancellationRequested)
        {
            var dispatchResult = await DispatchNextBatchAsync(stoppingToken).ConfigureAwait(false);
            switch (dispatchResult)
            {
                case DispatchBatchResult.BatchDispatched:
                    break;
                case DispatchBatchResult.NoBatchToDispatch _:
                    _logger.LogDebug("The dispatcher queue is empty, will sleep before the next poll");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    break;
                case DispatchBatchResult.DispatchError(var error):
                    _logger.LogError(error, "Error while dispatching messages");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    break;
            }
        }
        _logger.LogInformation("Outbox dispatcher service has stopped");
    }

    private async Task<DispatchBatchResult> DispatchNextBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _outboxBackend.BeginTransaction();
        try
        {
            var batch = await _outboxBackend.Read(10, stoppingToken).ConfigureAwait(false);
            if (batch.Length == 0) return new DispatchBatchResult.NoBatchToDispatch();

            var messagesToProduce = batch
                .Select(x => new BatchProduceItem(
                    topic: x.TopicPartition.Topic,
                    messageKey: x.Message.Key,
                    messageValue: x.Message.Value,
                    headers: BuildHeaders(x)))
                .ToList();


            await _producer.BatchProduceAsync(messagesToProduce).ConfigureAwait(false);

            scope.Complete();
            return new DispatchBatchResult.BatchDispatched(batch.Length);
        }
        catch (Exception ex)
        {
            return new DispatchBatchResult.DispatchError(ex);
        }
    }

    private MessageHeaders? BuildHeaders(OutboxRecord record) =>
        record.Message.Headers == null ? null : new MessageHeaders(record.Message.Headers);


    private abstract record DispatchBatchResult
    {
        public sealed record BatchDispatched(int BatchSize) : DispatchBatchResult;
        public sealed record NoBatchToDispatch : DispatchBatchResult;
        public sealed record DispatchError(Exception Exception) : DispatchBatchResult;
    }

}

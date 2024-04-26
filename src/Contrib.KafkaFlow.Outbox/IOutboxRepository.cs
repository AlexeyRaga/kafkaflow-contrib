namespace KafkaFlow.Outbox;

public interface IOutboxRepository
{
    ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default);
    Task<IEnumerable<OutboxTableRow>> Read(int batchSize, CancellationToken token = default);
}

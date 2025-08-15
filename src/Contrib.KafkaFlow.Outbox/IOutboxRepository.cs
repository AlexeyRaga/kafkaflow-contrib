using System.Transactions;

namespace KafkaFlow.Outbox;

public interface IOutboxRepository
{
    ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default);
    Task<IEnumerable<OutboxTableRow>> Read(int batchSize, CancellationToken token = default);


    /// <summary>
    /// Creates a new transaction scope for the outbox operations.
    /// By default, it uses a new transaction scope with the `RequiresNew` option,
    /// but repositories can override this method to provide a custom transaction scope.
    /// </summary>
    /// <remarks>
    /// Not all the backends support TransactionScope and can enlist in an ambient transaction.
    /// For example, the MongoDB backend cannot do it, as MongoDB has its own transaction management.
    /// Having this method allows the repository to provide a custom transaction scope, which can then
    /// be respected by the dispatcher service to ensure that the outbox operations are executed
    /// in the same transaction as the Kafka producer operations.
    /// </remarks>
    ITransactionScope BeginTransaction()
    {
        return new SystemTransactionScope(new(
            scopeOption: TransactionScopeOption.RequiresNew,
            transactionOptions: new TransactionOptions
                { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled));
    }
}

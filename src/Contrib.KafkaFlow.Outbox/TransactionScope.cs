using System.Transactions;

namespace KafkaFlow.Outbox;

public interface ITransactionScope : IDisposable
{
    void Complete();
}

public sealed class SystemTransactionScope(TransactionScope scope) : ITransactionScope
{
    public void Complete() => scope.Complete();
    public void Dispose() => scope.Dispose();

    public static ITransactionScope Create(TimeSpan? timeout = null) =>
        new SystemTransactionScope(new(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions
                { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = timeout ?? TimeSpan.FromSeconds(30) },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled));
}

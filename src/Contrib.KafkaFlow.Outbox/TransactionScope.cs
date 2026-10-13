using System.Transactions;

namespace KafkaFlow.Outbox;

public interface ITransactionScope : IDisposable
{
    void Complete();

    /// <summary>
    /// False once the transaction has aborted (e.g. timed out), so its work will be rolled back.
    /// </summary>
    bool IsActive => true;
}

public sealed class SystemTransactionScope(TransactionScope scope) : ITransactionScope
{
    private readonly Transaction? _transaction = Transaction.Current;

    public bool IsActive => _transaction?.TransactionInformation.Status is null or TransactionStatus.Active;
    public void Complete() => scope.Complete();
    public void Dispose() => scope.Dispose();

    public static ITransactionScope Create(TimeSpan? timeout = null) =>
        new SystemTransactionScope(new(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions
                { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = timeout ?? TimeSpan.FromSeconds(30) },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled));
}

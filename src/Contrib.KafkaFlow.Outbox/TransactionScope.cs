using System.Transactions;

namespace KafkaFlow.Outbox;

public interface ITransactionScope : IDisposable
{
    void Complete();
}

internal sealed class WrappedTransactionScope(TransactionScope scope) : ITransactionScope
{
    public void Complete() => scope.Complete();
    public void Dispose() => scope.Dispose();
}

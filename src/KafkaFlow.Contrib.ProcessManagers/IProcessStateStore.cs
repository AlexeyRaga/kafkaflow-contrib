namespace KafkaFlow.ProcessManagers;

public sealed class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException(string? message) : base(message)
    {
    }

    public OptimisticConcurrencyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public interface IProcessStateStore
{
    ValueTask Persist(Type processType, Guid processId, MarkedState state);
    ValueTask<MarkedState> Load(Type processType, Guid processId);

    ValueTask Delete(Type processType, Guid processId);
}

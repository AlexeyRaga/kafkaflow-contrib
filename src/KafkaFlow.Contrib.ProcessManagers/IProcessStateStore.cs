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
    /// <summary>
    /// Persists the given process state.
    /// Checks if the "marker" matches before overwriting the existing state.
    /// Throws <see cref="T:OptimisticConcurrencyException"/> if markers don't match, which means that another process
    /// may have updated the state.
    /// </summary>
    /// <exception cref="OptimisticConcurrencyException">When the state marker doesn't match the expected one while overwriting</exception>
    ValueTask Persist(Type processType, Guid processId, MarkedState state);

    ValueTask<MarkedState> Load(Type processType, Guid processId);

    ValueTask Delete(Type processType, Guid processId);
}

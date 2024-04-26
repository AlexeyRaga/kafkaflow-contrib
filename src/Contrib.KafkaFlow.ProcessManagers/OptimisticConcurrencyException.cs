namespace KafkaFlow.ProcessManagers;

public sealed class OptimisticConcurrencyException : Exception
{
    public Type ProcessType { get; init; }
    public Guid ProcessId { get; init; }

    public OptimisticConcurrencyException(Type processType, Guid processId, string? message) : base(message)
    {
        ProcessType = processType;
        ProcessId = processId;
    }

    public OptimisticConcurrencyException(Type processType, Guid processId, string? message, Exception? innerException) : base(message, innerException)
    {
        ProcessType = processType;
        ProcessId = processId;
    }
}

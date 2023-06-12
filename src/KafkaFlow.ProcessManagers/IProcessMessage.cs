using System.Diagnostics.CodeAnalysis;

namespace KafkaFlow.ProcessManagers;

public interface IProcessMessage
{
}

public enum ProcessResult
{
    StateUpdated,
    ProcessCompleted,
    StateNoChange
}

public interface IProcessMessage<T> : IProcessMessage
{
    Guid GetProcessId(T message);
    Task<ProcessResult> Handle(IMessageContext context, T message);
}

public interface IProcessManager
{
    Type StateType { get; }
}

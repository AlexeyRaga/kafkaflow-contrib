using System.Diagnostics.CodeAnalysis;

namespace KafkaFlow.ProcessManagers;

public interface IProcessMessage
{
}

public interface IProcessMessage<T> : IProcessMessage
{
    Guid GetProcessId(T message);
    Task Handle(IMessageContext context, T message);
}

public interface IProcessManager
{
    Type StateType { get; }
}

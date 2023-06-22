namespace KafkaFlow.ProcessManagers;

public interface IProcessMessage
{
}

public interface IProcessMessage<in T> : IProcessMessage
{
    Guid GetProcessId(T message);
    Task Handle(IMessageContext context, T message);
}

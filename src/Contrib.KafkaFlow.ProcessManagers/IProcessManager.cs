namespace KafkaFlow.ProcessManagers;

public interface IProcessManager
{
    Type StateType { get; }
}

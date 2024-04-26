
namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public interface IKafkaFlowFixture
{
    LoggingProcessStateStore ProcessStateStore { get; }
    IMessageProducer Producer { get; }
    string TopicName { get; }
}
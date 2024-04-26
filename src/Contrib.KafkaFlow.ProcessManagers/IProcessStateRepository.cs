namespace KafkaFlow.ProcessManagers;

public interface IProcessStateRepository
{
    ValueTask<int> Persist(Type processType, string processState, Guid processId, VersionedState state);
    ValueTask<IEnumerable<ProcessStateTableRow>> Load(Type processType, Guid processId);
    ValueTask<int> Delete(Type processType, Guid processId, int version);
}

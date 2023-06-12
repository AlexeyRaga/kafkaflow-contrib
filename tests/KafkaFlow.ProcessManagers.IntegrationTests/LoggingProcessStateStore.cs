using KafkaFlow.ProcessManagers.InMemory;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class LoggingProcessStateStore : IProcessStateRepository
{
    public enum ActionType
    {
        Persisted, Deleted
    }
    private readonly InMemoryProcessStateRepository _innerStore = new();
    private readonly List<(ActionType, Type, Guid, MarkedState?)> _log = new();

    public IReadOnlyList<(ActionType, Type, Guid, MarkedState?)> Changes => _log.AsReadOnly();
    public IReadOnlyDictionary<(Type, Guid), MarkedState> Current => _innerStore.Store.AsReadOnly();

    public ValueTask Persist(Type processType, Guid processId, MarkedState state)
    {
        _log.Add((ActionType.Persisted, processType, processId, state));
        return _innerStore.Persist(processType, processId, state);
    }

    public ValueTask<MarkedState> Load(Type processType, Guid processId)
    {
        return _innerStore.Load(processType, processId);
    }

    public ValueTask Delete(Type processType, Guid processId)
    {
        _log.Add((ActionType.Deleted, processType, processId, null));
        return _innerStore.Delete(processType, processId);
    }
}

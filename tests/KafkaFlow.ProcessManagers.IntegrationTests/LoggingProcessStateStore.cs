using KafkaFlow.ProcessManagers.InMemory;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class LoggingProcessStateStore : IProcessStateStore
{
    public enum ActionType
    {
        Persisted, Deleted
    }
    private readonly InMemoryProcessStateStore _innerStore = new();
    private readonly List<(ActionType, Type, Guid, VersionedState?)> _log = new();

    public IReadOnlyList<(ActionType, Type, Guid, VersionedState?)> Changes => _log.AsReadOnly();
    public IReadOnlyDictionary<(Type, Guid), VersionedState> Current => _innerStore.Store.AsReadOnly();

    public ValueTask Persist(Type processType, Guid processId, VersionedState state)
    {
        _log.Add((ActionType.Persisted, processType, processId, state));
        return _innerStore.Persist(processType, processId, state);
    }

    public ValueTask<VersionedState> Load(Type processType, Guid processId)
    {
        return _innerStore.Load(processType, processId);
    }

    public async ValueTask Delete(Type processType, Guid processId, ulong version)
    {
        await _innerStore.Delete(processType, processId, version);
        _log.Add((ActionType.Deleted, processType, processId, null));
    }
}

using System.Collections.Concurrent;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public sealed class LoggingProcessStateStore(IProcessStateStore innerStore) : IProcessStateStore
{
    public enum ActionType
    {
        Persisted, Deleted
    }
    private readonly IProcessStateStore _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
    private readonly ConcurrentQueue<(ActionType, Type, Guid, VersionedState?)> _log = [];

    public IReadOnlyList<(ActionType, Type, Guid, VersionedState?)> Changes => _log.ToList().AsReadOnly();

    public void ClearChanges() => _log.Clear();

    public ValueTask Persist(Type processType, Guid processId, VersionedState state)
    {
        _log.Enqueue((ActionType.Persisted, processType, processId, state));
        return _innerStore.Persist(processType, processId, state);
    }

    public ValueTask<VersionedState> Load(Type processType, Guid processId) => _innerStore.Load(processType, processId);

    public async ValueTask Delete(Type processType, Guid processId, int version)
    {
        await _innerStore.Delete(processType, processId, version);
        _log.Enqueue((ActionType.Deleted, processType, processId, null));
    }
}

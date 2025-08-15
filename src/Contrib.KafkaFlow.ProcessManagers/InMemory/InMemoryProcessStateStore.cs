using System.Collections.Concurrent;
using KafkaFlow.Outbox;

namespace KafkaFlow.ProcessManagers.InMemory;

public sealed class InMemoryProcessStateStore : IProcessStateStore
{
    public readonly ConcurrentDictionary<(Type, Guid), VersionedState> Store = new();

    public ValueTask Persist(Type processType, Guid processId, VersionedState state)
    {
        if (state.State == null)
        {
            Store.TryRemove((processType, processId), out _);
        }
        else
        {
            Store.AddOrUpdate((processType, processId), state, (_, currentState) =>
            {
                return currentState.Version != state.Version
                    ? throw new OptimisticConcurrencyException(processType, processId,
                        $"Concurrency error when persisting state {processType.FullName}")
                    : state;
            });
        }

        return default;
    }

    public ValueTask<VersionedState> Load(Type processType, Guid processId) => Store.TryGetValue((processType, processId), out var state)
            ? new ValueTask<VersionedState>(state)
            : new ValueTask<VersionedState>(new VersionedState(0, default));

    public ValueTask Delete(Type processType, Guid processId, int version)
    {
        Store.TryRemove((processType, processId), out _);
        return default;
    }

    public ITransactionScope CreateTransactionScope(TimeSpan timeout) =>
        SystemTransactionScope.Create(timeout);
}

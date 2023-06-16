using System.Collections.Concurrent;

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
                if (currentState.Version != state.Version)
                    throw new OptimisticConcurrencyException(processType, processId,
                        $"Concurrency error when persisting state {processType.FullName}");

                return state;
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<VersionedState> Load(Type processType, Guid processId)
    {
        return Store.TryGetValue((processType, processId), out var state)
            ? ValueTask.FromResult(state)
            : ValueTask.FromResult(new VersionedState(0, default));
    }

    public ValueTask Delete(Type processType, Guid processId, long version)
    {
        Store.TryRemove((processType, processId), out _);
        return ValueTask.CompletedTask;
    }
}

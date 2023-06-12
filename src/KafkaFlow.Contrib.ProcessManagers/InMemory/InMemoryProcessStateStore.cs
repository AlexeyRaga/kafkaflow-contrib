using System.Collections.Concurrent;

namespace KafkaFlow.ProcessManagers.InMemory;

public sealed class InMemoryProcessStateStore : IProcessStateStore
{
    public readonly ConcurrentDictionary<(Type, Guid), MarkedState> Store = new();

    public ValueTask Persist(Type processType, Guid processId, MarkedState state)
    {
        if (state.State == null)
        {
            Store.TryRemove((processType, processId), out _);
        }
        else
        {
            Store.AddOrUpdate((processType, processId), state, (_, currentState) =>
            {
                if (currentState.Marker != state.Marker)
                    throw new OptimisticConcurrencyException(
                        $"Concurrency error when persisting state {processType.FullName}");

                return state;
            });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<MarkedState> Load(Type processType, Guid processId)
    {
        return Store.TryGetValue((processType, processId), out var state)
            ? ValueTask.FromResult(state)
            : ValueTask.FromResult(new MarkedState(Guid.NewGuid(), default));
    }

    public ValueTask Delete(Type processType, Guid processId)
    {
        Store.TryRemove((processType, processId), out _);
        return ValueTask.CompletedTask;
    }
}

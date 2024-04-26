using System.Text.Json;

namespace KafkaFlow.ProcessManagers;

public sealed class ProcessManagersStore(IProcessStateRepository processStateRepository) : IProcessStateStore
{
    private readonly IProcessStateRepository _processStateRepository = processStateRepository;

    public async ValueTask Persist(Type processType, Guid processId, VersionedState state)
    {
        var persisted = await _processStateRepository.Persist(processType, JsonSerializer.Serialize(state.State), processId, state).ConfigureAwait(false);
        if (persisted == 0)
        {
            throw new OptimisticConcurrencyException(processType, processId,
                $"Concurrency error when persisting state {processType.FullName}");
        }
    }

    public async ValueTask<VersionedState> Load(Type processType, Guid processId)
    {
        var result = await _processStateRepository.Load(processType, processId).ConfigureAwait(false);
        var firstResult = result?.FirstOrDefault();

        if (firstResult == null)
        {
            return VersionedState.Zero;
        }

        var deserialized = JsonSerializer.Deserialize(firstResult.ProcessState, processType);
        return new VersionedState(firstResult.Version, deserialized);
    }

    public async ValueTask Delete(Type processType, Guid processId, int version)
    {
        var result = await _processStateRepository.Delete(processType, processId, version).ConfigureAwait(false);

        if (result == 0)
        {
            throw new OptimisticConcurrencyException(processType, processId,
                $"Concurrency error when persisting state {processType.FullName}");
        }
    }
}

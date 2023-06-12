using KafkaFlow.ProcessManagers;

namespace KafkaFlow.Contrib.ProcessManagers.Postgres;

public sealed class PostgresProcessManagersStore : IProcessStateStore
{
    public PostgresProcessManagersStore()
    {

    }

    public ValueTask Persist(Type processType, Guid processId, MarkedState state)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MarkedState> Load(Type processType, Guid processId)
    {
        throw new NotImplementedException();
    }

    public ValueTask Delete(Type processType, Guid processId)
    {
        throw new NotImplementedException();
    }
}

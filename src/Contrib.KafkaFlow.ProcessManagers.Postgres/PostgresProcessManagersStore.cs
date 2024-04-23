using System.Text.Json;
using Dapper;
using Npgsql;

namespace KafkaFlow.ProcessManagers.Postgres;

public sealed class PostgresProcessManagersStore(NpgsqlDataSource connectionPool) : IProcessStateStore
{
    private readonly NpgsqlDataSource _connectionPool = connectionPool;

    public async ValueTask Persist(Type processType, Guid processId, VersionedState state)
    {
        var sql = @"
INSERT INTO process_managers.processes(process_type, process_id, process_state)
VALUES (@process_type, @process_id, @process_state)
ON CONFLICT (process_type, process_id) DO
UPDATE
SET
    process_state       = EXCLUDED.process_state,
    date_updated_utc   = (now() AT TIME ZONE 'utc')
WHERE xmin = @version
";
        await using var conn = _connectionPool.CreateConnection();
        var result = await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            process_state = JsonSerializer.Serialize(state.State),
            version = state.Version
        });

        if (result == 0)
            throw new OptimisticConcurrencyException(processType, processId,
                $"Concurrency error when persisting state {processType.FullName}");
    }

    public async ValueTask<VersionedState> Load(Type processType, Guid processId)
    {
        var sql = @"
SELECT process_state, xmin as version
FROM process_managers.processes
WHERE process_type = @process_type AND process_id = @process_id";

        await using var conn = _connectionPool.CreateConnection();
        var result = await conn.QueryAsync<ProcessStateRow>(sql, new
        {
            process_type = processType.FullName,
            process_id = processId
        });

        var firstResult = result?.FirstOrDefault();

        if (firstResult == null) return VersionedState.Zero;

        var decoded = JsonSerializer.Deserialize(firstResult.process_state, processType);
        return new VersionedState(firstResult.version, decoded);
    }

    public async ValueTask Delete(Type processType, Guid processId, int version)
    {
        var sql = @"
DELETE FROM process_managers.processes
WHERE process_type = @process_type AND process_id = @process_id and xmin = @version";

        await using var conn = _connectionPool.CreateConnection();
        var result = await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            version = version
        });

        if (result == 0)
            throw new OptimisticConcurrencyException(processType, processId,
                $"Concurrency error when persisting state {processType.FullName}");
    }

    private sealed class ProcessStateRow
    {
        public string process_state { get; set; } = null!;
        public int version { get; set; }
    }
}

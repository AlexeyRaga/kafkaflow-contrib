using Dapper;
using Npgsql;

namespace KafkaFlow.ProcessManagers.Postgres;

public sealed class PostgresProcessStateRepository(NpgsqlDataSource connectionPool) : IProcessStateRepository
{
    private readonly NpgsqlDataSource _connectionPool = connectionPool;

    public async ValueTask<int> Persist(Type processType, string processState, Guid processId, VersionedState state)
    {
        var sql = """
            INSERT INTO process_managers.processes(process_type, process_id, process_state)
            VALUES (@process_type, @process_id, @process_state)
            ON CONFLICT (process_type, process_id) DO
            UPDATE
            SET
                process_state    = EXCLUDED.process_state,
                date_updated_utc = (now() AT TIME ZONE 'utc')
            WHERE xmin = @version
            """;
        await using var conn = _connectionPool.CreateConnection();
        return await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            process_state = processState,
            version = state.Version
        }).ConfigureAwait(false);
    }

    public async ValueTask<IEnumerable<ProcessStateTableRow>> Load(Type processType, Guid processId)
    {
        var sql = """
            SELECT process_state as "ProcessState", xmin as "Version"
            FROM process_managers.processes
            WHERE process_type = @process_type AND process_id = @process_id
            """;

        await using var conn = _connectionPool.CreateConnection();
        return (await conn.QueryAsync<(string ProcessState, uint Version)>(sql, new
        {
            process_type = processType.FullName,
            process_id = processId
        }).ConfigureAwait(false)).Select(v => new ProcessStateTableRow(v.ProcessState, (int)v.Version));
    }

    public async ValueTask<int> Delete(Type processType, Guid processId, int version)
    {
        var sql = """
            DELETE FROM process_managers.processes
            WHERE process_type = @process_type AND process_id = @process_id and xmin = @version
            """;

        await using var conn = _connectionPool.CreateConnection();
        return await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            version
        }).ConfigureAwait(false);
    }
}

using Dapper;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using System.Text.Json;

namespace KafkaFlow.ProcessManagers.SqlServer;

public sealed class SqlServerProcessManagersStore : IProcessStateStore
{
    private readonly SqlServerBackendOptions _options;

    public SqlServerProcessManagersStore(IOptions<SqlServerBackendOptions> options)
        => _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask Persist(Type processType, Guid processId, VersionedState state)
    {
        var sql = """
            MERGE INTO [processes] as [target]
            USING (VALUES (@process_type, @process_id,@process_state)) AS [source] ([process_type], [process_id], [process_state])
            ON [target].[process_type] = @process_type AND [target].[process_id] = @process_id
            WHEN MATCHED AND [target].[rowversion] = @Version THEN
             UPDATE SET
                [process_state] = @process_state,
                [date_updated_utc] = SYSUTCDATETIME(),
                [rowversion] = [target].[rowversion] + 1
            WHEN NOT MATCHED THEN
             INSERT ([process_type], [process_id], [process_state])
             VALUES (@process_type, @process_id,@process_state);
            """;

        using var conn = new SqlConnection(_options.ConnectionString);
        var result = await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            process_state = JsonSerializer.Serialize(state.State),
            version = state.Version
        });

        if (result == 0)
        {
            throw new OptimisticConcurrencyException(processType, processId,
                $"Concurrency error when persisting state {processType.FullName}");
        }
    }

    public async ValueTask<VersionedState> Load(Type processType, Guid processId)
    {
        var sql = """
            SELECT [process_state], [rowversion] as [version]
            FROM [processes]
            WHERE [process_type] = @process_type AND [process_id] = @process_id;
            """;

        using var conn = new SqlConnection(_options.ConnectionString);
        var result = await conn.QueryAsync<ProcessStateRow>(sql, new
        {
            process_type = processType.FullName,
            process_id = processId
        });

        var firstResult = result?.FirstOrDefault();

        if (firstResult == null)
        {
            return VersionedState.Zero;
        }

        var decoded = JsonSerializer.Deserialize(firstResult.process_state, processType);
        return new VersionedState(firstResult.version, decoded);
    }

    public async ValueTask Delete(Type processType, Guid processId, int version)
    {
        var sql = """
            DELETE FROM [processes]
            WHERE [process_type] = @process_type AND [process_id] = @process_id and [rowversion] = @version;
            """;

        using var conn = new SqlConnection(_options.ConnectionString);
        var result = await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            version
        });

        if (result == 0)
        {
            throw new OptimisticConcurrencyException(processType, processId,
                $"Concurrency error when persisting state {processType.FullName}");
        }
    }

    private sealed class ProcessStateRow
    {
        public required string process_state { get; set; }
        public required int version { get; set; }
    }
}

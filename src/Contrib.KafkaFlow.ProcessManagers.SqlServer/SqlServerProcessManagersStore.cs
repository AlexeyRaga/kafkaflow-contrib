using Dapper;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using KafkaFlow.Outbox;

namespace KafkaFlow.ProcessManagers.SqlServer;

public sealed class SqlServerProcessStateRepository(IOptions<SqlServerBackendOptions> options) : IProcessStateRepository
{
    private readonly SqlServerBackendOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask<int> Persist(Type processType, string processState, Guid processId, VersionedState state)
    {
        var sql = """
            MERGE INTO [process_managers].[processes] as [target]
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
            SELECT [process_state] as [ProcessState], [rowversion] as [Version]
            FROM [process_managers].[processes]
            WHERE [process_type] = @process_type AND [process_id] = @process_id;
            """;

        await using var conn = new SqlConnection(_options.ConnectionString);
        return await conn.QueryAsync<ProcessStateTableRow>(sql, new
        {
            process_type = processType.FullName,
            process_id = processId
        }).ConfigureAwait(false);
    }

    public async ValueTask<int> Delete(Type processType, Guid processId, int version)
    {
        var sql = """
            DELETE FROM [process_managers].[processes]
            WHERE [process_type] = @process_type AND [process_id] = @process_id and [rowversion] = @version;
            """;

        using var conn = new SqlConnection(_options.ConnectionString);
        return await conn.ExecuteAsync(sql, new
        {
            process_type = processType.FullName,
            process_id = processId,
            version
        }).ConfigureAwait(false);
    }

    public ITransactionScope CreateTransactionScope(TimeSpan timeout) =>
        SystemTransactionScope.Create(timeout);
}

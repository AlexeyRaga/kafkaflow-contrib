using Dapper;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace KafkaFlow.Outbox.SqlServer;

public class SqlServerOutboxRepository(IOptions<SqlServerBackendOptions> options) : IOutboxRepository
{
    private readonly SqlServerBackendOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default)
    {
        var sql = """
            INSERT INTO [outbox].[outbox] ([topic_name], [partition], [message_key], [message_headers], [message_body])
            VALUES (@topic_name, @partition, @message_key, @message_headers, @message_body);
            """;

        using var conn = new SqlConnection(_options.ConnectionString);
        await conn.ExecuteAsync(sql, new
        {
            topic_name = outboxTableRow.TopicName,
            partition = outboxTableRow.Partition,
            message_key = outboxTableRow.MessageKey,
            message_headers = outboxTableRow.MessageHeaders,
            message_body = outboxTableRow.MessageBody
        }).ConfigureAwait(false);
    }

    public async Task<IEnumerable<OutboxTableRow>> Read(int batchSize, CancellationToken token = default)
    {
        var sql = """
            DELETE FROM [outbox].[outbox]
            OUTPUT [DELETED].[sequence_id] as [SequenceId],
                [DELETED].[topic_name] as [TopicName],
                [DELETED].[partition] as [Partition],
                [DELETED].[message_key] as [MessageKey],
                [DELETED].[message_headers] as [MessageHeaders],
                [DELETED].[message_body] as [MessageBody]
            WHERE
                [sequence_id] IN (
                    SELECT TOP (@batch_size) [sequence_id] FROM [outbox].[outbox]
                    ORDER BY [sequence_id]
                );
            """;

        using var conn = new SqlConnection(_options.ConnectionString);
        return await conn.QueryAsync<OutboxTableRow>(sql, new { batch_size = batchSize }).ConfigureAwait(false);
    }
}

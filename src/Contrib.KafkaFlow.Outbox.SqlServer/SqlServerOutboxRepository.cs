using Dapper;
using System.Data.SqlClient;

namespace KafkaFlow.Outbox.SqlServer;

public class SqlServerOutboxRepository(string connectionString) : IOutboxRepository
{
    public async ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default)
    {
        var sql = """
            INSERT INTO [outbox].[outbox] ([topic_name], [partition], [message_key], [message_headers], [message_body])
            VALUES (@topic_name, @partition, @message_key, @message_headers, @message_body);
            """;

        await using var conn = new SqlConnection(connectionString);
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
        // Inset into a temporary table so we can guarantee the order is returned
        // by the sequence id
        var sql = """
            DECLARE @DeletedRows TABLE(
            	    [SequenceId] [bigint],
            	    [TopicName] [nvarchar](255) NOT NULL,
            	    [Partition] [int] NULL,
            	    [MessageKey] [varbinary](max) NULL,
            	    [MessageHeaders] [nvarchar](max) NULL,
            	    [MessageBody] [varbinary](max) NULL
                );

            DELETE FROM [outbox].[outbox]
            OUTPUT [DELETED].[sequence_id] as [SequenceId],
                [DELETED].[topic_name] as [TopicName],
                [DELETED].[partition] as [Partition],
                [DELETED].[message_key] as [MessageKey],
                [DELETED].[message_headers] as [MessageHeaders],
                [DELETED].[message_body] as [MessageBody]
            INTO @DeletedRows
            WHERE
                [sequence_id] IN (
                    SELECT TOP (@batch_size) [sequence_id] FROM [outbox].[outbox]
                    ORDER BY [sequence_id]
                );

            SELECT [SequenceId], [TopicName], [Partition], [MessageKey], [MessageHeaders], [MessageBody]
            FROM @DeletedRows
            ORDER BY [SequenceId];
            """;

        await using var conn = new SqlConnection(connectionString);
        return await conn.QueryAsync<OutboxTableRow>(sql, new { batch_size = batchSize }).ConfigureAwait(false);
    }
}

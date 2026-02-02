using Dapper;
using Npgsql;

namespace KafkaFlow.Outbox.Postgres;

public class PostgresOutboxRepository(NpgsqlDataSource connectionPool) : IOutboxRepository
{
    private readonly NpgsqlDataSource _connectionPool = connectionPool;

    public async ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default)
    {
        var sql = """
            INSERT INTO outbox.outbox(topic_name, partition, message_key, message_headers, message_body)
            VALUES (@topic_name, @partition, @message_key, @message_headers, @message_body)
            """;

        await using var conn = _connectionPool.CreateConnection();
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
            WITH messages AS (
                DELETE FROM outbox.outbox
                WHERE
                    sequence_id = ANY(ARRAY(
                        SELECT sequence_id FROM outbox.outbox
                        ORDER BY sequence_id
                        LIMIT @batch_size
                        FOR UPDATE
                    ))
                RETURNING
                    sequence_id as "SequenceId",
                    topic_name as "TopicName",
                    partition as "Partition",
                    message_key as "MessageKey",
                    message_headers as "MessageHeaders",
                    message_body as "MessageBody"
            )
            SELECT SequenceId, TopicName, Partition, MessageKey, MessageHeaders, MessageBody
            FROM messages
            ORDER BY SequenceId
            """;
        await using var conn = _connectionPool.CreateConnection();
        return await conn.QueryAsync<OutboxTableRow>(sql, new { batch_size = batchSize }).ConfigureAwait(false);
    }
}

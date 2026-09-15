using Dapper;
using Microsoft.Data.SqlClient;

namespace KafkaFlow.Outbox.SqlServer;

public class SqlServerOutboxRepository : IOutboxRepository
{
    private readonly string _connectionString;
    private readonly string _storeSql;
    private readonly string _readSql;

    public SqlServerOutboxRepository(string connectionString)
        : this(connectionString, new SqlServerOutboxOptions())
    {
    }

    public SqlServerOutboxRepository(string connectionString, SqlServerOutboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _connectionString = connectionString;

        var table = options.QualifiedTableName;

        _storeSql = $"""
            INSERT INTO {table} ([topic_name], [partition], [message_key], [message_headers], [message_body])
            VALUES (@topic_name, @partition, @message_key, @message_headers, @message_body);
            """;

        // Insert into a temporary table so we can guarantee the order is returned
        // by the sequence id
        _readSql = $"""
            DECLARE @DeletedRows TABLE(
            	    [SequenceId] [bigint],
            	    [TopicName] [nvarchar](255) NOT NULL,
            	    [Partition] [int] NULL,
            	    [MessageKey] [varbinary](max) NULL,
            	    [MessageHeaders] [nvarchar](max) NULL,
            	    [MessageBody] [varbinary](max) NULL
                );

            DELETE FROM {table}
            OUTPUT [DELETED].[sequence_id] as [SequenceId],
                [DELETED].[topic_name] as [TopicName],
                [DELETED].[partition] as [Partition],
                [DELETED].[message_key] as [MessageKey],
                [DELETED].[message_headers] as [MessageHeaders],
                [DELETED].[message_body] as [MessageBody]
            INTO @DeletedRows
            WHERE
                [sequence_id] IN (
                    SELECT TOP (@batch_size) [sequence_id]
                    FROM {table} WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                    ORDER BY [sequence_id]
                );

            SELECT [SequenceId], [TopicName], [Partition], [MessageKey], [MessageHeaders], [MessageBody]
            FROM @DeletedRows
            ORDER BY [SequenceId];
            """;
    }

    public async ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(_storeSql, new
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
        await using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<OutboxTableRow>(_readSql, new { batch_size = batchSize }).ConfigureAwait(false);
    }
}

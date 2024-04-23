using Confluent.Kafka;
using Dapper;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using System.Text.Json;

namespace KafkaFlow.Outbox.SqlServer;

public class SqlServerOutboxBackend(IOptions<SqlServerBackendOptions> options) : IOutboxBackend
{
    private readonly SqlServerBackendOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask Store(TopicPartition topicPartition, Message<byte[], byte[]> message, CancellationToken token = default)
    {
        var sql = """
            INSERT INTO [outbox].[outbox] ([topic_name], [partition], [message_key], [message_headers], [message_body])
            VALUES (@topic_name, @partition, @message_key, @message_headers, @message_body);
            """;

        using var conn = new SqlConnection(_options.ConnectionString);

        var rawHeaders =
            message.Headers == null
            ? null
            : JsonSerializer.Serialize(message.Headers.ToDictionary(x => x.Key, x => x.GetValueBytes()));

        var res = await conn.ExecuteAsync(sql, new
        {
            topic_name = topicPartition.Topic,
            partition = topicPartition.Partition.IsSpecial ? null : (int?)topicPartition.Partition.Value,
            message_key = message.Key,
            message_headers = rawHeaders,
            message_body = message.Value
        }).ConfigureAwait(false);

    }

    public async ValueTask<OutboxRecord[]> Read(int batchSize, CancellationToken token = default)
    {
        var sql = """
            DELETE FROM [outbox].[outbox]
            OUTPUT DELETED.*
            WHERE
                [sequence_id] IN (
                    SELECT TOP (@batch_size) [sequence_id] FROM [outbox].[outbox]
                    ORDER BY [sequence_id]
                );
            """;
        using var conn = new SqlConnection(_options.ConnectionString);
        var result = await conn.QueryAsync<OutboxTableRow>(sql, new { batch_size = batchSize });

        return result?.Select(ToOutboxRecord).ToArray() ?? Array.Empty<OutboxRecord>();
    }

    private static OutboxRecord ToOutboxRecord(OutboxTableRow row)
    {
        var partition = row.partition.HasValue ? new Partition(row.partition.Value) : Partition.Any;
        var topicPartition = new TopicPartition(row.topic_name, partition);

        var storedHeaders =
            row.message_headers == null
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, byte[]>>(row.message_headers);

        var headers =
            storedHeaders?.Aggregate(new Headers(), (h, x) =>
            {
                h.Add(x.Key, x.Value);
                return h;
            });

        var msg = new Message<byte[], byte[]>
        {
            Key = row.message_key!,
            Value = row.message_body!,
            Headers = headers
        };

        return new OutboxRecord(topicPartition, msg);
    }
}

internal sealed class OutboxTableRow
{
    public long sequence_id { get; set; }
    public string topic_name { get; set; } = null!;
    public int? partition { get; set; }
    public byte[]? message_key { get; set; }
    public string? message_headers { get; set; }
    public byte[]? message_body { get; set; }
}

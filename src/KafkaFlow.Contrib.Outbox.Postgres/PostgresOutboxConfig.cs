using System.ComponentModel.DataAnnotations;

namespace KafkaFlow.Outbox.Postgres;

public sealed class PostgresOutboxConfig
{
    [Required] public string ConnectionString { get; set; } = null!;

    public int WaitSeconds { get; set; } = 30;
}

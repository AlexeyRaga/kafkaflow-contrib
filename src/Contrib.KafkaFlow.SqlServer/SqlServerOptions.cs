namespace KafkaFlow.SqlServer;

public record SqlServerOptions
{
    public required string ConnectionString { get; init; }
}

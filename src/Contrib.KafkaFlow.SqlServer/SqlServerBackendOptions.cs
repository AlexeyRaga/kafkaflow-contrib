namespace KafkaFlow.SqlServer;

public record SqlServerBackendOptions
{
    public required string ConnectionString { get; set; }
}

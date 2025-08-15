using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerOutboxBackend(this IServiceCollection services, string connectionString) =>
        services
            .AddSingleton<IOutboxRepository, SqlServerOutboxRepository>(_ => new SqlServerOutboxRepository(connectionString))
            .AddSingleton<IOutboxBackend, OutboxBackend>();
}

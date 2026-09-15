using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerOutboxBackend(this IServiceCollection services, string connectionString) =>
        services.AddSqlServerOutboxBackend(connectionString, new SqlServerOutboxOptions());

    public static IServiceCollection AddSqlServerOutboxBackend(
        this IServiceCollection services,
        string connectionString,
        SqlServerOutboxOptions options) =>
        services
            .AddSingleton<IOutboxRepository, SqlServerOutboxRepository>(_ => new SqlServerOutboxRepository(connectionString, options))
            .AddSingleton<IOutboxBackend, OutboxBackend>();
}

using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerOutboxBackend(this IServiceCollection services) =>
        services.AddSingleton<IOutboxBackend, SqlServerOutboxBackend>();
}

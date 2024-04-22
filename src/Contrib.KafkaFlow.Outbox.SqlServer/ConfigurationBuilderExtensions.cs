using KafkaFlow;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerOutboxBackend(this IServiceCollection services) =>
        services.AddSingleton<IOutboxBackend, SqlServerOutboxBackend>();

    public static IServiceCollection AddSqlServerOutboxBackend(this IServiceCollection services, string connectionString)
    {
        services.ConfigureSqlServerBackend(options => options.ConnectionString = connectionString);
        return AddSqlServerOutboxBackend(services);
    }
}

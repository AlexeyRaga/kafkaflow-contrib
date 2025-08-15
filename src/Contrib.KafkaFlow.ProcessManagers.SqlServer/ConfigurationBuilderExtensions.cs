using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerProcessManagerState(this IServiceCollection services, string connectionString) =>
        services
            .AddSingleton<IProcessStateRepository, SqlServerProcessStateRepository>(_ => new SqlServerProcessStateRepository(connectionString))
            .AddSingleton<IProcessStateStore, ProcessManagersStore>();
}

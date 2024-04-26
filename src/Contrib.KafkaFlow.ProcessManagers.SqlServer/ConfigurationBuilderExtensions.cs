using KafkaFlow;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerProcessManagerState(this IServiceCollection services) =>
        services.AddSingleton<IProcessStateStore, SqlServerProcessManagersStore>();


    public static IServiceCollection AddSqlServerProcessManagerState(this IServiceCollection services, string connectionString)
    {
        services.ConfigureSqlServerBackend(options => options.ConnectionString = connectionString);
        return AddSqlServerProcessManagerState(services);
    }
}

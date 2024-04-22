using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.SqlServer;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddSqlServerProcessManagerState(this IServiceCollection services) =>
        services.AddSingleton<IProcessStateStore, SqlServerProcessManagersStore>();
}

using KafkaFlow.ProcessManagers;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Contrib.ProcessManagers.Postgres;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddPostgresProcessManagerState(this IServiceCollection services) =>
        services.AddSingleton<IProcessStateStore, PostgresProcessManagersStore>();
}

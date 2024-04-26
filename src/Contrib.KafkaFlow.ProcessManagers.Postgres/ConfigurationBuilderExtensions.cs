using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.Postgres;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddPostgresProcessManagerState(this IServiceCollection services) =>
        services
            .AddSingleton<IProcessStateRepository, PostgresProcessStateRepository>()
            .AddSingleton<IProcessStateStore, ProcessManagersStore>();
}

using KafkaFlow.ProcessManagers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Contrib.ProcessManagers.Postgres;

public sealed class PostgresProcessManagersConfig
{
    public string ConnectionString { get; set; }
}

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddPostgresProcessManagerState(this IServiceCollection services)
    {
        services
            .AddOptions<PostgresProcessManagersConfig>()
            .Configure<IConfiguration>((opt, config) => config.GetRequiredSection("ProcessManagers").Bind(opt))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddSingleton<IProcessStateStore, PostgresProcessManagersStore>();

        return services;
    }
}

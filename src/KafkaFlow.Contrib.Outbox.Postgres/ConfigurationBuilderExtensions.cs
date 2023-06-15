using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.Postgres;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddPostgresOutboxBackend(this IServiceCollection services)
    {
        services
            .AddOptions<PostgresOutboxConfig>()
            .Configure<IConfiguration>((opt, config) => config.GetRequiredSection("Outbox").Bind(opt))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddSingleton<IOutboxBackend, PostgresOutboxBackend>();

        return services;
    }
}

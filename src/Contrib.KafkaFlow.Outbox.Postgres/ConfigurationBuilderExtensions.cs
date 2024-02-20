using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.Postgres;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddPostgresOutboxBackend(this IServiceCollection services) =>
        services.AddSingleton<IOutboxBackend, PostgresOutboxBackend>();
}

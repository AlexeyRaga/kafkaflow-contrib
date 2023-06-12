using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.Postgres;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddInMemoryOutboxBackend(this IServiceCollection services) =>
        services.AddSingleton<IOutboxBackend, PostgresOutboxBackend>();
}

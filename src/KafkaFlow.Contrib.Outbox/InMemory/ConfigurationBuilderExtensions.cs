using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.Outbox.InMemory;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddInMemoryOutboxBackend(this IServiceCollection services) =>
        services.AddSingleton<IOutboxBackend, InMemoryOutboxBackend>();
}

using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.InMemory;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddInMemoryProcessManagerState(this IServiceCollection services) =>
        services
            .AddSingleton<IProcessStateStore, InMemoryProcessStateStore>();
}

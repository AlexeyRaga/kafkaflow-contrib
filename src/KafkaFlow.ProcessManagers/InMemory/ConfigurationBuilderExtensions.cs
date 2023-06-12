using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.InMemory;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddInMemoryProcessManagerState(this IServiceCollection services) =>
        services
            .AddSingleton<IProcessStateRepository, InMemoryProcessStateRepository>();

    public static IServiceCollection AddProcessManagerState(
        this IServiceCollection services,
        Func<IServiceProvider, IProcessStateRepository> factory) => services.AddSingleton(factory);

    public static IServiceCollection AddProcessManagerState(
        this IServiceCollection services,
        IProcessStateRepository repository) => services.AddSingleton(repository);
}

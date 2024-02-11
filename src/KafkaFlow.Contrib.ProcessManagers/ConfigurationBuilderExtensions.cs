using KafkaFlow.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers;

public static class ConfigurationBuilderExtensions
{
    public static IConsumerMiddlewareConfigurationBuilder AddProcessManagers(
        this IConsumerMiddlewareConfigurationBuilder builder,
        Action<ProcessManagerConfigurationBuilder> configure)
    {
        var processManagerBuilder = new ProcessManagerConfigurationBuilder(builder.DependencyConfigurator);
        configure(processManagerBuilder);

        var configuration = processManagerBuilder.Build();

        return
            builder.Add(
                resolver => new ProcessManagerMiddleware(
                    resolver,
                    resolver.Resolve<IProcessStateStore>(),
                    configuration),
                MiddlewareLifetime.Message);
    }

    public static IServiceCollection AddProcessManagerStateStore(
        this IServiceCollection services,
        Func<IServiceProvider, IProcessStateStore> factory) => services.AddSingleton(factory);

    public static IServiceCollection AddProcessManagerStateStore(
        this IServiceCollection services,
        IProcessStateStore store) => services.AddSingleton(store);
}

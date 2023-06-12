using KafkaFlow.Configuration;

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
                    resolver.Resolve<IProcessStateRepository>(),
                    configuration),
                MiddlewareLifetime.Scoped);
    }
}

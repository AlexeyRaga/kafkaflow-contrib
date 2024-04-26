using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureSqlServerBackend(this IServiceCollection services, IConfiguration configuration)
        => services.Configure<SqlServerBackendOptions>(configuration);

    public static IServiceCollection ConfigureSqlServerBackend(this IServiceCollection services, Action<SqlServerBackendOptions> configureOptions)
        => services.Configure<SqlServerBackendOptions>(configureOptions);

    public static IServiceCollection ConfigureSqlServerBackend(this IServiceCollection services, IConfiguration configuration, Action<BinderOptions>? configureBinder)
        => services.Configure<SqlServerBackendOptions>(configuration, configureBinder);

    public static IServiceCollection ConfigureSqlServerBackend(this IServiceCollection services, string? name, IConfiguration configuration)
        => services.Configure<SqlServerBackendOptions>(name, configuration);

    public static IServiceCollection ConfigureSqlServerBackend(this IServiceCollection services, string? name, Action<SqlServerBackendOptions> configureOptions)
        => services.Configure<SqlServerBackendOptions>(name, configureOptions);

    public static IServiceCollection ConfigureSqlServerBackend(this IServiceCollection services, string? name, IConfiguration configuration, Action<BinderOptions>? configureBinder)
        => services.Configure<SqlServerBackendOptions>(name, configuration, configureBinder);
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureSqlServer(this IServiceCollection services, IConfiguration configuration)
        => services.Configure<SqlServerOptions>(configuration);

    public static IServiceCollection ConfigureSqlServer(this IServiceCollection services, Action<SqlServerOptions> configureOptions)
        => services.Configure<SqlServerOptions>(configureOptions);

    public static IServiceCollection ConfigureSqlServer(this IServiceCollection services, IConfiguration configuration, Action<BinderOptions>? configureBinder)
        => services.Configure<SqlServerOptions>(configuration, configureBinder);

    public static IServiceCollection ConfigureSqlServer(this IServiceCollection services, string? name, IConfiguration configuration)
        => services.Configure<SqlServerOptions>(name, configuration);

    public static IServiceCollection ConfigureSqlServer(this IServiceCollection services, string? name, Action<SqlServerOptions> configureOptions)
        => services.Configure<SqlServerOptions>(name, configureOptions);

    public static IServiceCollection ConfigureSqlServer(this IServiceCollection services, string? name, IConfiguration configuration, Action<BinderOptions>? configureBinder)
        => services.Configure<SqlServerOptions>(name, configuration, configureBinder);
}

using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <exception cref="InvalidOperationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services)
        where TDecorator : TService =>
        services == null
            ? throw new ArgumentNullException(nameof(services))
            : services.DecorateDescriptors(typeof(TService), x => x.Decorate(typeof(TDecorator)));

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorate">Function to create a decorator</param>
    /// <exception cref="InvalidOperationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<IServiceProvider, TService, TService> decorate)
    {
        ArgumentNullException.ThrowIfNull(decorate);

        return decorate == null
            ? throw new ArgumentNullException(nameof(decorate))
            : services == null
            ? throw new ArgumentNullException(nameof(services))
            : services.DecorateDescriptors(typeof(TService), x => x.Decorate((provider, inner) => decorate(provider, (TService)inner)!));
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorate">Function to create a decorator</param>
    /// <exception cref="InvalidOperationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, TService> decorate) =>
        services.Decorate<TService>((_, inner) => decorate(inner));

    private static IServiceCollection DecorateDescriptors(this IServiceCollection services, Type serviceType,
        Func<ServiceDescriptor, ServiceDescriptor> decorator) =>
        services.TryDecorateDescriptors(serviceType, decorator)
            ? services
            : throw new InvalidOperationException($"No service of type '{serviceType.FullName}' has been registered");

    private static bool TryDecorateDescriptors(this IServiceCollection services, Type serviceType, Func<ServiceDescriptor, ServiceDescriptor> decorator)
    {
        if (!services.TryGetDescriptors(serviceType, out var descriptors))
        {
            return false;
        }

        foreach (var descriptor in descriptors)
        {
            var index = services.IndexOf(descriptor);

            // To avoid reordering descriptors, in case a specific order is expected.
            services.Insert(index, decorator(descriptor));

            services.Remove(descriptor);
        }

        return true;
    }

    private static bool TryGetDescriptors(this IServiceCollection services, Type serviceType, out ICollection<ServiceDescriptor> descriptors) =>
        (descriptors = services.Where(service => service.ServiceType == serviceType).ToArray()).Count != 0;

    private static ServiceDescriptor Decorate(this ServiceDescriptor descriptor, Type decoratorType) =>
        descriptor.WithFactory(provider => provider.CreateInstance(decoratorType, provider.GetInstance(descriptor)));

    private static ServiceDescriptor Decorate(this ServiceDescriptor descriptor, Func<IServiceProvider, object, object> decorate) =>
        descriptor.WithFactory(provider =>
        {
            var inner = provider.GetInstance(descriptor);
            return decorate(provider, inner);
        });

    private static ServiceDescriptor WithFactory(this ServiceDescriptor descriptor, Func<IServiceProvider, object> factory) =>
        ServiceDescriptor.Describe(descriptor.ServiceType, factory, descriptor.Lifetime);

    private static object GetInstance(this IServiceProvider provider, ServiceDescriptor descriptor)
        => descriptor.ImplementationInstance ?? (descriptor.ImplementationType != null
            ? provider.GetServiceOrCreateInstance(descriptor.ImplementationType)
            : descriptor.ImplementationFactory == null
            ? throw new InvalidOperationException($"ImplementationFactory for '{descriptor.ServiceType.FullName}' is not set")
            : descriptor.ImplementationFactory(provider));

    private static object GetServiceOrCreateInstance(this IServiceProvider provider, Type type) =>
        ActivatorUtilities.GetServiceOrCreateInstance(provider, type);

    private static object CreateInstance(this IServiceProvider provider, Type type, params object[] arguments) =>
        ActivatorUtilities.CreateInstance(provider, type, arguments);
}

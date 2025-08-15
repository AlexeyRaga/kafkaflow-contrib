using System.ComponentModel;
using System.Reflection;

namespace KafkaFlow.ProcessManagers;

public sealed class ProcessManagerConfigurationBuilder(IDependencyConfigurator dependencyConfigurator)
{
    private readonly IDependencyConfigurator _dependencyConfigurator = dependencyConfigurator ?? throw new ArgumentNullException(nameof(dependencyConfigurator));
    private InstanceLifetime _serviceLifetime = InstanceLifetime.Transient;
    private readonly List<Type> _processManagers = [];
    private TransactionMode _transactionMode = TransactionMode.ForEachHandler;
    private TimeSpan _transactionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Specify how transactions should behave for process managers
    /// </summary>
    /// <exception cref="InvalidEnumArgumentException">When a provided mode is not a valid member of an enum</exception>
    public ProcessManagerConfigurationBuilder WithTransactionMode(TransactionMode transactionMode)
    {
        if (!Enum.IsDefined(typeof(TransactionMode), transactionMode))
        {
            throw new InvalidEnumArgumentException(nameof(transactionMode), (int)transactionMode, typeof(TransactionMode));
        }

        _transactionMode = transactionMode;
        return this;
    }

    public ProcessManagerConfigurationBuilder WithTransactionTimeout(TimeSpan transactionTimeout)
    {
        if (transactionTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(transactionTimeout), "Transaction timeout must be greater than zero.");

        _transactionTimeout = transactionTimeout;
        return this;
    }

    /// <summary>
    /// Set the handler lifetime. The default value is <see cref="InstanceLifetime.Transient"/>
    /// </summary>
    /// <param name="lifetime">The <see cref="InstanceLifetime"/> enum value</param>
    /// <returns></returns>
    public ProcessManagerConfigurationBuilder WithInstanceLifetime(InstanceLifetime lifetime)
    {
        _serviceLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Register a process manager
    /// </summary>
    /// <typeparam name="T">A process manager to add</typeparam>
    public ProcessManagerConfigurationBuilder AddProcessManager<T>() where T : IProcessManager
    {
        _processManagers.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Register a process manager
    /// </summary>
    /// <param name="type">A process manager to add</param>
    /// <exception cref="InvalidOperationException">When a given type is not a process manager</exception>
    public ProcessManagerConfigurationBuilder AddProcessManager(Type type)
    {
        if (!typeof(IProcessManager).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Type {type.FullName} is not a ProcessManager");
        }

        _processManagers.Add(type);
        return this;
    }

    /// <summary>
    /// Discover all the process managers from all the assemblies of specified types
    /// </summary>
    /// <param name="assemblyMarkerTypes">Types from whose assemblies process managers should be discovered</param>
    public ProcessManagerConfigurationBuilder AddProcessManagersFromAssemblyOf(params Type[] assemblyMarkerTypes)
    {
        var targetTypes =
            assemblyMarkerTypes
                .SelectMany(x => x.GetTypeInfo().Assembly.GetTypes())
                .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IProcessManager).IsAssignableFrom(x))
                .Distinct();
        _processManagers.AddRange(targetTypes);
        return this;
    }

    /// <summary>
    /// Discover all the process managers from all the assemblies of specified types
    /// </summary>
    /// <typeparam name="T">A type marker for the assembly to discover process managers from</typeparam>
    public ProcessManagerConfigurationBuilder AddProcessManagersFromAssemblyOf<T>() =>
        AddProcessManagersFromAssemblyOf(typeof(T));

    internal ProcessManagerConfiguration Build()
    {
        var maps = (
                from processType in _processManagers
                from messageType in GetMessageTypes(processType)
                group processType by messageType)
            .ToDictionary(x => x.Key, x => x.ToList());

        var mapping = new HandlerTypeMapping(maps);

        foreach (var processType in _processManagers)
        {
            _dependencyConfigurator.Add(processType, processType, _serviceLifetime);
        }

        return new ProcessManagerConfiguration(_transactionMode, mapping, _transactionTimeout);
    }

    private static List<Type> GetMessageTypes(Type processType) =>
        processType
            .GetInterfaces()
            .Where(x => x.IsGenericType && typeof(IProcessMessage).IsAssignableFrom(x))
            .Select(x => x.GenericTypeArguments[0])
            .ToList();
}

using System.Transactions;

namespace KafkaFlow.ProcessManagers;


/// <summary>
/// How to run transactions for process managers
/// </summary>
public enum TransactionMode
{
    /// <summary>
    /// Do not run process managers in transactions
    /// </summary>
    Disabled,

    /// <summary>
    /// Each message handler runs in each own transaction scope
    /// </summary>
    ForEachHandler,

    /// <summary>
    /// All message handlers for a given messages run in one transaction scope
    /// </summary>
    ForAllHandlers
}

internal sealed record ProcessManagerConfiguration(
    TransactionMode TransactionMode,
    HandlerTypeMapping TypeMapping,
    Func<TransactionScope> BeginTransaction);

internal sealed record HandlerTypeMapping(IReadOnlyDictionary<Type, List<Type>> Mapping)
{
    private static readonly IReadOnlyList<Type> EmptyList = new List<Type>().AsReadOnly();
    public IReadOnlyList<Type> GetHandlersTypes(Type messageType) =>
        Mapping.TryGetValue(messageType, out var handlerType) ? handlerType : EmptyList;
}

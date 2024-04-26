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

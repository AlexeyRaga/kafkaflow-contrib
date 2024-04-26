using System.Transactions;

namespace KafkaFlow.ProcessManagers;

internal sealed record ProcessManagerConfiguration(
    TransactionMode TransactionMode,
    HandlerTypeMapping TypeMapping,
    Func<TransactionScope> BeginTransaction);

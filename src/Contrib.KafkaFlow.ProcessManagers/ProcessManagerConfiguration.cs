using System.Transactions;
using KafkaFlow.Outbox;

namespace KafkaFlow.ProcessManagers;

internal sealed record ProcessManagerConfiguration(
    TransactionMode TransactionMode,
    HandlerTypeMapping TypeMapping,
    TimeSpan TransactionTimeout);

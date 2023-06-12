namespace KafkaFlow.ProcessManagers;

public readonly record struct MarkedState(Guid Marker, object? State);
namespace KafkaFlow.ProcessManagers;

public readonly record struct VersionedState(int Version, object? State)
{
    public static readonly VersionedState Zero = new(0, null);
};

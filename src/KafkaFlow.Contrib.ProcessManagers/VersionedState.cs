namespace KafkaFlow.ProcessManagers;

public readonly record struct VersionedState(int Version, object? State)
{
    public static VersionedState Zero = new VersionedState(0, null);
};

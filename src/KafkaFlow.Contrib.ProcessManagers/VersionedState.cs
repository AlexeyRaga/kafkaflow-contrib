namespace KafkaFlow.ProcessManagers;

public readonly record struct VersionedState(ulong Version, object? State)
{
    public static VersionedState Zero = new VersionedState(0, null);
};

namespace KafkaFlow.ProcessManagers;

internal sealed record HandlerTypeMapping(IReadOnlyDictionary<Type, List<Type>> Mapping)
{
    private static readonly IReadOnlyList<Type> EmptyList = new List<Type>().AsReadOnly();
    public IReadOnlyList<Type> GetHandlersTypes(Type messageType) =>
        Mapping.TryGetValue(messageType, out var handlerType) ? handlerType : EmptyList;
}

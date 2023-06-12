using System.Collections.Concurrent;

namespace KafkaFlow.ProcessManagers;

internal abstract class HandlerExecutor
{
    private static readonly ConcurrentDictionary<Type, HandlerExecutor> Handlers = new();

    public static HandlerExecutor GetExecutor(Type stateType, Type messageType)
    {
        return Handlers.GetOrAdd(messageType,
            _ => (HandlerExecutor)Activator.CreateInstance(
                typeof(TypedHandlerExecutor<,>).MakeGenericType(stateType, messageType))!);
    }

    public abstract Task<(ProcessResult, object?)> Execute(object handler, object? state, IMessageContext context, object message);
    public abstract Guid GetProcessId(object handler, object message);

    private class TypedHandlerExecutor<TState, TMessage> : HandlerExecutor where TState: class
    {
        public override Guid GetProcessId(object handler, object message) =>
            ((IProcessMessage<TMessage>)handler).GetProcessId((TMessage)message);

        public override async Task<(ProcessResult, object?)> Execute(object handler, object? state, IMessageContext context, object message)
        {
            var processManager = (ProcessManager<TState>)handler;
            processManager.SetState((TState?)state);

            var result = await ((IProcessMessage<TMessage>)handler).Handle(context, (TMessage)message);
            return (result, processManager.State);
        }
    }
}

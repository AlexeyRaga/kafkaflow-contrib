using System.ComponentModel;
using System.Transactions;

namespace KafkaFlow.ProcessManagers;

internal sealed class ProcessManagerMiddleware : IMessageMiddleware
{
    private readonly IDependencyResolver _dependencyResolver;
    private readonly IProcessStateStore _stateStore;
    private readonly ProcessManagerConfiguration _configuration;

    public ProcessManagerMiddleware(
        IDependencyResolver dependencyResolver,
        IProcessStateStore stateStore,
        ProcessManagerConfiguration configuration)
    {
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        if (context.Message.Value != null)
        {
            var handlers = _configuration.TypeMapping.GetHandlersTypes(context.Message.Value.GetType());
            if (handlers.Any())
            {
                var allHandlersScope = StartTransactionScopeFor(TransactionMode.ForAllHandlers);
                await Task.WhenAll(handlers.Select(t => RunHandler(t, context))).ConfigureAwait(false);
                allHandlersScope?.Complete();
            }
        }

        await next(context).ConfigureAwait(false);
    }

    private TransactionScope? StartTransactionScopeFor(TransactionMode mode) =>
        mode == _configuration.TransactionMode ? _configuration.BeginTransaction() : null;


    private async Task RunHandler(Type handlerType, IMessageContext context)
    {
        var handler = _dependencyResolver.Resolve(handlerType);
        var stateType = ((IProcessManager)handler).StateType;
        var messageType = context.Message.Value.GetType();

        var executor = HandlerExecutor.GetExecutor(stateType, messageType);

        using var transactionScope = StartTransactionScopeFor(TransactionMode.ForEachHandler);
        var processId = executor.GetProcessId(handler, context.Message.Value);
        var state = await _stateStore.Load(stateType, processId).ConfigureAwait(false);

        var newState = await executor.Execute(handler, state.State, context, context.Message.Value);
        if (newState == null)
        {
            await _stateStore.Delete(stateType, processId);
        }
        else
        {
            await _stateStore.Persist(stateType, processId, state with { State = newState });
        }

        transactionScope?.Complete();
    }
}

using System.ComponentModel;
using System.Transactions;

namespace KafkaFlow.ProcessManagers;

internal sealed class ProcessManagerMiddleware : IMessageMiddleware
{
    private readonly IDependencyResolver _dependencyResolver;
    private readonly IProcessStateRepository _stateRepository;
    private readonly ProcessManagerConfiguration _configuration;

    public ProcessManagerMiddleware(
        IDependencyResolver dependencyResolver,
        IProcessStateRepository stateRepository,
        ProcessManagerConfiguration configuration)
    {
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
        _stateRepository = stateRepository ?? throw new ArgumentNullException(nameof(stateRepository));
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

        var transactionScope = StartTransactionScopeFor(TransactionMode.ForEachHandler);
        var processId = executor.GetProcessId(handler, context.Message.Value);
        var state = await _stateRepository.Load(stateType, processId).ConfigureAwait(false);

        var (result, newState) = await executor.Execute(handler, state.State, context, context.Message.Value);

        switch (result)
        {
            case ProcessResult.StateNoChange: break;
            case ProcessResult.StateUpdated:
                await _stateRepository.Persist(stateType, processId, state with { State = newState });
                break;
            case ProcessResult.ProcessCompleted:
                await _stateRepository.Delete(stateType, processId);
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(ProcessResult));
        }
        transactionScope?.Complete();
    }
}

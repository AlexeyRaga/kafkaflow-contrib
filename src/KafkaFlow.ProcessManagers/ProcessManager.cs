namespace KafkaFlow.ProcessManagers;

public abstract class ProcessManager<TState> : IProcessManager where TState : class
{
    public Type StateType => typeof(TState);
    protected internal TState? State { get; private set; }

    protected bool IsStateSet => State != null;

    internal void SetState(TState? state)
    {
        State = state;
    }

    protected ProcessResult UpdateState(TState state)
    {
        State = state;
        return ProcessResult.StateUpdated;
    }

    protected ProcessResult FinishProcess()
    {
        State = null;
        return ProcessResult.ProcessCompleted;
    }

    protected ProcessResult NoStateChange() => ProcessResult.StateNoChange;

    protected async Task<ProcessResult> WithRequiredStateAsync(Func<TState, Task<ProcessResult>> handler)
    {
        if (!IsStateSet) return NoStateChange();
        return await handler(State!);
    }

    protected ProcessResult WithRequiredState(Func<TState, ProcessResult> handler)
    {
        if (!IsStateSet) return NoStateChange();
        return handler(State!);
    }
}

namespace KafkaFlow.ProcessManagers;

public interface IProcessManager
{
    Type StateType { get; }
}

public abstract class ProcessManager<TState> : IProcessManager where TState : class
{
    public Type StateType => typeof(TState);
    protected internal TState? State { get; private set; }

    protected bool IsStateSet => State != null;

    internal void SetState(TState? state)
    {
        State = state;
    }

    protected void UpdateState(TState state)
    {
        State = state;
    }

    protected void FinishProcess()
    {
        State = null;
    }

    protected async Task WithRequiredStateAsync(Func<TState, Task> handler)
    {
        if (!IsStateSet) return;
        await handler(State!);
    }

    protected void WithRequiredState(Action<TState> handler)
    {
        if (!IsStateSet) return;
        handler(State!);
    }
}

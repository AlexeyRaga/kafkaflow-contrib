using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace KafkaFlow.ProcessManagers.IntegrationTests.UserLifeCycle;

public sealed record TestState(DateTimeOffset StartedAt, ImmutableList<string> Log);

public sealed record UserRegistered(Guid UserId, string Email);

public sealed record UserApproved(Guid UserId);

public sealed record UserAccessGranted(Guid UserId);

// ReSharper disable once UnusedType.Global
public class UserLifeCycleProcess(ILogger<UserLifeCycleProcess> logger, IMessageProducer<IOutboxedMessageProducer> producer) : ProcessManager<TestState>,
    IProcessMessage<UserRegistered>,
    IProcessMessage<UserAccessGranted>,
    IProcessMessage<UserApproved>
{
    public Guid GetProcessId(UserRegistered message) => message.UserId;
    public Guid GetProcessId(UserAccessGranted message) => message.UserId;
    public Guid GetProcessId(UserApproved message) => message.UserId;

    public async Task Handle(IMessageContext context, UserRegistered message)
    {
        logger.LogInformation("Received message: {Message}", message);
        await producer.ProduceAsync(message.UserId.ToString(), new UserApproved(message.UserId));
        await producer.ProduceAsync(message.UserId.ToString(), new UserAccessGranted(message.UserId));

        var newState = new TestState(DateTimeOffset.UtcNow, ["UserRegistered"]);
        UpdateState(newState);
    }

    public async Task Handle(IMessageContext context, UserApproved message)
    {
        logger.LogInformation("Received message: {Message}", message);

        await WithRequiredStateAsync(state =>
        {
            var newState = state with { Log = state.Log.Add("UserApproved") };
            UpdateState(newState);
            return Task.CompletedTask;
        });
    }

    public Task Handle(IMessageContext context, UserAccessGranted message)
    {
        logger.LogInformation("Received message: {Message}", message);
        FinishProcess();
        return Task.CompletedTask;
    }
}

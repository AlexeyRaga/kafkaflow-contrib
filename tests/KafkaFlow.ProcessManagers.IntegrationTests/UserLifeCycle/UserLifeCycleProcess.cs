using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace KafkaFlow.ProcessManagers.IntegrationTests.UserLifeCycle;

public sealed record TestState(DateTimeOffset StartedAt, ImmutableList<string> Log);

public sealed record UserRegistered(Guid UserId, string Email);

public sealed record UserApproved(Guid UserId);

public sealed record UserAccessGranted(Guid UserId);

// ReSharper disable once UnusedType.Global
public class UserLifeCycleProcess(ILogger<UserLifeCycleProcess> logger, IMessageProducer<ITestMessageProducer> producer) : ProcessManager<TestState>,
    IProcessMessage<UserRegistered>,
    IProcessMessage<UserAccessGranted>,
    IProcessMessage<UserApproved>
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMessageProducer<ITestMessageProducer> _producer = producer ?? throw new ArgumentNullException(nameof(producer));

    public Guid GetProcessId(UserRegistered message) => message.UserId;
    public Guid GetProcessId(UserAccessGranted message) => message.UserId;
    public Guid GetProcessId(UserApproved message) => message.UserId;

    public async Task Handle(IMessageContext context, UserRegistered message)
    {
        _logger.LogInformation("Received message: {Message}", message);
        await _producer.ProduceAsync(message.UserId.ToString(), new UserApproved(message.UserId));
        await _producer.ProduceAsync(message.UserId.ToString(), new UserAccessGranted(message.UserId));

        var newState = new TestState(DateTimeOffset.UtcNow, ["UserRegistered"]);
        UpdateState(newState);
    }

    public async Task Handle(IMessageContext context, UserApproved message)
    {
        _logger.LogInformation("Received message: {Message}", message);

        await WithRequiredStateAsync(state =>
        {
            var newState = state with { Log = state.Log.Add("UserApproved") };
            UpdateState(newState);
            return Task.CompletedTask;
        });
    }

    public Task Handle(IMessageContext context, UserAccessGranted message)
    {
        _logger.LogInformation("Received message: {Message}", message);
        FinishProcess();
        return Task.CompletedTask;
    }
}

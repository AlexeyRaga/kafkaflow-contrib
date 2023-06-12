using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed record TestState(DateTimeOffset StartedAt, ImmutableList<string> Log);

public sealed class UserRegistered
{
    public UserRegistered(Guid UserId, string Email)
    {
        this.UserId = UserId;
        this.Email = Email;
    }

    public Guid UserId { get; init; }
    public string Email { get; init; }

    public void Deconstruct(out Guid UserId, out string Email)
    {
        UserId = this.UserId;
        Email = this.Email;
    }
}

public sealed record UserApproved(Guid UserId);

public sealed record UserAccessGranted(Guid UserId);

public class TestProcessManager : ProcessManager<TestState>,
    IProcessMessage<UserRegistered>,
    IProcessMessage<UserAccessGranted>,
    IProcessMessage<UserApproved>
{
    private readonly ILogger _logger;
    private readonly IMessageProducer<ITestMessageProducer> _producer;

    public TestProcessManager(ILogger<TestProcessManager> logger, IMessageProducer<ITestMessageProducer> producer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    }

    public Guid GetProcessId(UserRegistered message) => message.UserId;
    public Guid GetProcessId(UserAccessGranted message) => message.UserId;
    public Guid GetProcessId(UserApproved message) => message.UserId;

    public async Task Handle(IMessageContext context, UserRegistered message)
    {
        _logger.LogInformation("Received message: {Message}", message);
        await _producer.ProduceAsync(message.UserId.ToString(), new UserApproved(message.UserId));
        await _producer.ProduceAsync(message.UserId.ToString(), new UserAccessGranted(message.UserId));

        var newState = new TestState(DateTimeOffset.UtcNow, ImmutableList.Create("UserRegistered"));
        UpdateState(newState);
    }

    public async Task Handle(IMessageContext context, UserApproved message)
    {
        _logger.LogInformation("Received message: {Message}", message);
        WithRequiredState(state =>
        {
            var newState = state with { Log = state.Log.Add("UserApproved") };
            UpdateState(newState);
        });
    }

    public async Task Handle(IMessageContext context, UserAccessGranted message)
    {
        _logger.LogInformation("Received message: {Message}", message);
        FinishProcess();
    }
}

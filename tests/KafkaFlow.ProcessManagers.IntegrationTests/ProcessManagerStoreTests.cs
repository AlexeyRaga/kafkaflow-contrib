using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public abstract class ProcessManagerStoreTests
{
    public abstract void Configure(IConfiguration config, IServiceCollection services);

    [Fact]
    public async Task Should_write_update_and_delete_state()
    {
        var processId = Guid.NewGuid();
        var state = processId.ToString();

        var config =
            new ConfigurationManager()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

        var services = new ServiceCollection();
        Configure(config, services);
        var sp = services.BuildServiceProvider();

        var store = sp.GetRequiredService<IProcessStateStore>();

        var noState = await store.Load(state.GetType(), processId);
        noState.Should().BeEquivalentTo(VersionedState.Zero);

        await store.Persist(state.GetType(), processId, new VersionedState(0, state));

        var hasState = await store.Load(state.GetType(), processId);
        hasState.State.Should().NotBeNull();
        hasState.Version.Should().BePositive();

        await store.Delete(state.GetType(), processId, hasState.Version);
        var goneState = await store.Load(state.GetType(), processId);
        goneState.Should().BeEquivalentTo(VersionedState.Zero);
    }

}

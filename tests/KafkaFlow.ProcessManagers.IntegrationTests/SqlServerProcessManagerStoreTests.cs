using FluentAssertions;
using KafkaFlow.ProcessManagers.SqlServer;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KafkaFlow.ProcessManagers.IntegrationTests;
public sealed class SqlServerProcessManagerStoreTests
{
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

        var connStr = config.GetConnectionString("SqlServerBackend");

        var store = new SqlServerProcessManagersStore(Options.Create(new SqlServerOptions { ConnectionString = connStr }));

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

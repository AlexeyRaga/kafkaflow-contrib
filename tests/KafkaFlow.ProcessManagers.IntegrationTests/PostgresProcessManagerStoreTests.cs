using FluentAssertions;
using KafkaFlow.Contrib.ProcessManagers.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class PostgresProcessManagerStoreTests
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

        var pgConfig = config.GetSection("ProcessManagers").Get<PostgresProcessManagersConfig>();

        var opt = Options.Create(pgConfig!);

        var pool = new NpgsqlDataSourceBuilder(pgConfig!.ConnectionString).Build();

        var store = new PostgresProcessManagersStore(pool);

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

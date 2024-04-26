using KafkaFlow.ProcessManagers.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class PostgresProcessManagerStoreTests : ProcessManagerStoreTests
{
    public override void Configure(IConfiguration config, IServiceCollection services)
    {
        var connStr = config.GetConnectionString("PostgresBackend");
        var pool = new NpgsqlDataSourceBuilder(connStr).Build();
        services.AddSingleton(pool);
        services.AddPostgresProcessManagerState();
    }
}

using KafkaFlow.Outbox.Postgres;
using KafkaFlow.ProcessManagers.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public class PostgresKafkaFlowFixture : KafkaFlowFixture<PostgresKafkaFlowFixture>
{
    public PostgresKafkaFlowFixture()
        : base("pg", ["localhost:9092"]) { }

    public override void ConfigureFixture(IConfiguration config, IServiceCollection services)
    {
        var connStr = config.GetConnectionString("PostgresBackend");
        var pool = new NpgsqlDataSourceBuilder(connStr).Build();

        services
            .AddSingleton(pool)
            .AddPostgresProcessManagerState()
            .AddPostgresOutboxBackend();
    }
}

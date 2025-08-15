using KafkaFlow.Outbox.SqlServer;
using KafkaFlow.ProcessManagers.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public sealed class SqlServerKafkaFlowFixture : KafkaFlowFixture<SqlServerKafkaFlowFixture>
{
    public SqlServerKafkaFlowFixture()
        : base("mssql", ["localhost:9092"]) { }

    public override void ConfigureFixture(IConfiguration config, IServiceCollection services)
        => services
            .AddSqlServerProcessManagerState(config.GetConnectionString("SqlServerBackend")!)
            .AddSqlServerOutboxBackend(config.GetConnectionString("SqlServerBackend")!);
}

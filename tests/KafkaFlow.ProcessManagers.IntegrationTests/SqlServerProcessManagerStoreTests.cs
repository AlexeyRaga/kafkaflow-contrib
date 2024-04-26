using KafkaFlow.ProcessManagers.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public sealed class SqlServerProcessManagerStoreTests : ProcessManagerStoreTests
{
    public override void Configure(IConfiguration config, IServiceCollection services) => services.AddSqlServerProcessManagerState(config.GetConnectionString("SqlServerBackend")!);
}

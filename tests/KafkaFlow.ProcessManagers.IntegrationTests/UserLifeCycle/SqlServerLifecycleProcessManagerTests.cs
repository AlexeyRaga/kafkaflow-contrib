using KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

namespace KafkaFlow.ProcessManagers.IntegrationTests.UserLifeCycle;

public sealed class SqlServerLifecycleProcessManagerTests(SqlServerKafkaFlowFixture fixture) : UserLifecycleProcessManagerTests<SqlServerKafkaFlowFixture>(fixture)
{ }

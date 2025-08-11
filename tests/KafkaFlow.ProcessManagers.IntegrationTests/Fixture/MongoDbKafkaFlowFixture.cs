using Contrib.KafkaFlow.Outbox.MongoDb;
using Contrib.KafkaFlow.ProcessManagers.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public class MongoDbKafkaFlowFixture : KafkaFlowFixture<MongoDbKafkaFlowFixture>
{
    public MongoDbKafkaFlowFixture()
        : base("mongoDb", ["localhost:9092"]) { }

    public override void ConfigureFixture(IConfiguration config, IServiceCollection services)
    {
        var connectionString = config.GetConnectionString("MongoDbServerBackend");
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("kafkaflow_test");

        services
            .AddMongoDbOutboxBackend(database)
            .AddMongoDbProcessManagerState(database);
    }
}

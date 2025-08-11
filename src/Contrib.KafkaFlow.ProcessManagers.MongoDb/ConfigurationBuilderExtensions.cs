using KafkaFlow.ProcessManagers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.ProcessManagers.MongoDb;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddMongoDbProcessManagerState(this IServiceCollection services, IMongoDatabase database, string collectionName = "process_states") =>
        services
            .AddSingleton<IProcessStateRepository>(_ => new MongoDbProcessStateRepository(database, collectionName))
            .AddSingleton<IProcessStateStore, ProcessManagersStore>();
}

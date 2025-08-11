using KafkaFlow.Outbox;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddMongoDbOutboxBackend(this IServiceCollection services, IMongoDatabase database, string collectionName = "outbox") =>
        services
            .AddSingleton<IOutboxRepository>(_ => new MongoDbOutboxRepository(database, collectionName))
            .AddSingleton<IOutboxBackend, OutboxBackend>();
}

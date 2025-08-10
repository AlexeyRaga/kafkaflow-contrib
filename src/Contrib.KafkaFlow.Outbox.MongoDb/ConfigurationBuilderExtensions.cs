using Contrib.KafkaFlow.Outbox.MongoDb;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace KafkaFlow.Outbox.MongoDb;

public static class ConfigurationBuilderExtensions
{
    public static IServiceCollection AddMongoDbOutboxBackend(this IServiceCollection services, IMongoDatabase database, string collectionName = "outbox") =>
        services
            .AddSingleton<IOutboxRepository>(_ => new MongoDbOutboxRepository(database, collectionName))
            .AddSingleton<IOutboxBackend, OutboxBackend>();

    public static IServiceCollection AddMongoDbOutboxBackend(this IServiceCollection services, Func<IServiceProvider, IMongoDatabase> databaseFactory, string collectionName = "outbox") =>
        services
            .AddSingleton<IOutboxRepository>(provider => new MongoDbOutboxRepository(databaseFactory(provider), collectionName))
            .AddSingleton<IOutboxBackend, OutboxBackend>();
}

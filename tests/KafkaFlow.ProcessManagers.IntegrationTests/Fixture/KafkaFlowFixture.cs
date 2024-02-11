using Confluent.Kafka;
using KafkaFlow.Consumers;
using KafkaFlow.Contrib.ProcessManagers.Postgres;
using KafkaFlow.Outbox;
using KafkaFlow.Outbox.Postgres;
using KafkaFlow.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public class KafkaFlowFixture : IDisposable, IAsyncDisposable
{
    public readonly string FixtureId = Guid.NewGuid().ToString();
    public string TopicName { get; }
    public readonly ServiceProvider ServiceProvider;
    private readonly IKafkaBus _kafkaBus;
    private readonly CancellationTokenSource _fixtureCancellation;

    public LoggingProcessStateStore ProcessStateStore { get; }

    public IMessageProducer Producer { get; }

    public KafkaFlowFixture()
    {
        _fixtureCancellation = new CancellationTokenSource();
        TopicName = $"messages-{FixtureId}";

        var services = new ServiceCollection();

        var config = new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connStr = config.GetConnectionString("PostgresBackend");
        var pool = new NpgsqlDataSourceBuilder(connStr).Build();

        services
            .AddSingleton<IConfiguration>(config)
            .AddSingleton(pool)
            .AddLogging(log => log.AddConsole().AddDebug())
            .AddPostgresProcessManagerState()
            .Decorate<IProcessStateStore, LoggingProcessStateStore>()
            .AddPostgresOutboxBackend()
            .AddKafka(kafka =>
                kafka
                    .UseMicrosoftLog()
                    .AddCluster(cluster =>
                        cluster
                            .WithBrokers(new[] { "localhost:9092 " })
                            .CreateTopicIfNotExists(TopicName, 3, 1)
                            .AddOutboxDispatcher(x => x.WithPartitioner(Partitioner.Murmur2Random))
                            .AddProducer<KafkaFlowFixture>(producer =>
                                producer
                                    .WithOutbox()
                                    .DefaultTopic(TopicName)
                                    .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>()))
                            .AddProducer<ITestMessageProducer>(producer =>
                                producer
                                    .WithOutbox()
                                    .DefaultTopic(TopicName)
                                    .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>())
                                )
                            .AddConsumer(consumer =>
                                consumer
                                    .Topic(TopicName)
                                    .WithGroupId($"group-{FixtureId}")
                                    .WithBufferSize(100)
                                    .WithWorkersCount(1)
                                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                                    .AddMiddlewares(middlewares =>
                                        middlewares
                                            .AddDeserializer<JsonCoreDeserializer>()
                                            .AddProcessManagers(pm => pm.AddProcessManagersFromAssemblyOf<KafkaFlowFixture>())
                                        )
                            )
                    )
            );
        ServiceProvider = services.BuildServiceProvider();

        ProcessStateStore = (LoggingProcessStateStore)ServiceProvider.GetRequiredService<IProcessStateStore>();

        Producer = ServiceProvider.GetRequiredService<IMessageProducer<KafkaFlowFixture>>();

        var svc = ServiceProvider.GetServices<IHostedService>();

        foreach (var service in svc)
        {
            service.StartAsync(_fixtureCancellation.Token);
        }

        _kafkaBus = ServiceProvider.CreateKafkaBus();
        _kafkaBus.StartAsync(_fixtureCancellation.Token);
    }

    public void Dispose()
    {
        if (!_disposedAsync)
        {
            DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    private bool _disposedAsync = false;

    public async ValueTask DisposeAsync()
    {
        _disposedAsync = true;

        _fixtureCancellation.Cancel();
        _fixtureCancellation.Dispose();
        await _kafkaBus.StopAsync();

        foreach (var cons in ServiceProvider.GetRequiredService<IConsumerAccessor>().All)
        {
            await cons.StopAsync();
        }

        await ServiceProvider.DisposeAsync();
    }
}

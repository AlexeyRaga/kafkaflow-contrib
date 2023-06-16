using KafkaFlow.Consumers;
using KafkaFlow.Contrib.ProcessManagers.Postgres;
using KafkaFlow.Outbox;
using KafkaFlow.Outbox.InMemory;
using KafkaFlow.Outbox.Postgres;
using KafkaFlow.ProcessManagers.InMemory;
using KafkaFlow.Serializer;
using KafkaFlow.TypedHandler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public class KafkaFlowFixture : IDisposable, IAsyncDisposable
{
    public readonly string FixtureId = Guid.NewGuid().ToString();
    public string TopicName { get; }
    public readonly ServiceProvider ServiceProvider;
    private readonly IKafkaBus _kafkaBus;
    private readonly CancellationTokenSource _fixtureCancellation;

    public LoggingProcessStateStore ProcessStateStore { get; }

    public IMessageProducer Producer { get; init; }

    public KafkaFlowFixture()
    {
        _fixtureCancellation = new CancellationTokenSource();
        TopicName = $"messages-{FixtureId}";

        var services = new ServiceCollection();
        ProcessStateStore = new LoggingProcessStateStore();

        var config = new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        services
            .AddSingleton<IConfiguration>(config.Build())
            .AddLogging(log => log.AddConsole().AddDebug())
            .AddPostgresProcessManagerState()
            // .AddProcessManagerStateStore(ProcessStateStore)
            .AddPostgresOutboxBackend()
            .AddKafka(kafka =>
                kafka
                    .UseMicrosoftLog()
                    .AddCluster(cluster =>
                        cluster
                            .WithBrokers(new[] { "localhost:9092 " })
                            .CreateTopicIfNotExists(TopicName, 3, 1)
                            .AddOutboxDispatcher()
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
                                            .AddSerializer<JsonCoreSerializer>()
                                            .AddProcessManagers(pm => pm.AddProcessManagersFromAssemblyOf<KafkaFlowFixture>())
                                        )
                            )
                    )
            );
        ServiceProvider = services.BuildServiceProvider();

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

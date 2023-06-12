using KafkaFlow.Consumers;
using KafkaFlow.Outbox.InMemory;
using KafkaFlow.ProcessManagers.InMemory;
using KafkaFlow.Serializer;
using KafkaFlow.TypedHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public class KafkaFlowFixture : IDisposable, IAsyncDisposable
{
    public readonly string FixtureId = Guid.NewGuid().ToString();
    public string TopicName { get; }
    public readonly ServiceProvider ServiceProvider;
    private readonly IKafkaBus _kafkaBus;

    public LoggingProcessStateStore ProcessStateStore { get; }

    public IMessageProducer Producer { get; init; }

    public KafkaFlowFixture()
    {
        TopicName = $"messages-{FixtureId}";

        var services = new ServiceCollection();
        ProcessStateStore = new LoggingProcessStateStore();

        services
            .AddLogging(log => log.AddConsole().AddDebug())
            .AddProcessManagerStateStore(ProcessStateStore)
            .AddInMemoryOutboxBackend()
            .AddKafka(kafka =>
                kafka
                    .UseMicrosoftLog()
                    .AddCluster(cluster =>
                        cluster
                            .WithBrokers(new[] { "localhost:9092 " })
                            .CreateTopicIfNotExists(TopicName, 3, 1)
                            .AddProducer<KafkaFlowFixture>(producer =>
                                producer
                                    .DefaultTopic(TopicName)
                                    .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>()))
                            .AddProducer<ITestMessageProducer>(producer =>
                                producer
                                    .DefaultTopic(TopicName)
                                    // .WithOutbox()
                                    .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>()))
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

        _kafkaBus = ServiceProvider.CreateKafkaBus();
        _kafkaBus.StartAsync();
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

        await _kafkaBus.StopAsync();

        foreach (var cons in ServiceProvider.GetRequiredService<IConsumerAccessor>().All)
        {
            await cons.StopAsync();
        }

        await ServiceProvider.DisposeAsync();
    }
}

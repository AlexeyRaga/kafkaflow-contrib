using Confluent.Kafka;
using KafkaFlow.Consumers;
using KafkaFlow.Outbox;
using KafkaFlow.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public abstract class KafkaFlowFixture<T>(
    string prefix,
    IEnumerable<string> brokers,
    int numberOfPartitions = 3,
    short replicationFactor = 1,
    Partitioner partitioner = Partitioner.Murmur2Random,
    int bufferSize = 100,
    int workersCount = 1
    ) : KafkaFlowFixture<T, JsonCoreSerializer, JsonCoreDeserializer>(prefix, brokers, numberOfPartitions, replicationFactor, partitioner, bufferSize, workersCount)
{ }

public abstract class KafkaFlowFixture<T, TSerializer, TDeserializer> : IDisposable, IAsyncDisposable, IKafkaFlowFixture where TSerializer : class, ISerializer
    where TDeserializer : class, IDeserializer
{
    public readonly string FixtureId = Guid.NewGuid().ToString();
    public string TopicName { get; }
    public readonly ServiceProvider ServiceProvider;
    private readonly IKafkaBus _kafkaBus;
    private readonly CancellationTokenSource _fixtureCancellation;

    public LoggingProcessStateStore ProcessStateStore { get; }

    public IMessageProducer Producer { get; }

    public abstract void ConfigureFixture(IConfiguration config, IServiceCollection services);

    public KafkaFlowFixture(string prefix,
        IEnumerable<string> brokers,
        int numberOfPartitions = 3,
        short replicationFactor = 1,
        Partitioner partitioner = Partitioner.Murmur2Random,
        int bufferSize = 100,
        int workersCount = 1)
    {
        _fixtureCancellation = new CancellationTokenSource();
        TopicName = $"{prefix}-messages-{FixtureId}";

        var services = new ServiceCollection();

        var config = new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        ConfigureFixture(config, services);
        services
            .AddSingleton<IConfiguration>(config)
            .AddLogging(log => log.AddConsole().AddDebug())
            .Decorate<IProcessStateStore, LoggingProcessStateStore>()
            .AddKafka(kafka =>
                kafka
                    .UseMicrosoftLog()
                    .AddCluster(cluster =>
                        cluster
                            .WithBrokers(brokers)
                            .CreateTopicIfNotExists(TopicName, numberOfPartitions, replicationFactor)
                            .AddOutboxDispatcher(x => x.WithPartitioner(partitioner))
                            .AddProducer<IUnOutboxedMessageProducer>(producer =>
                                producer
                                    .DefaultTopic(TopicName)
                                    .AddMiddlewares(m => m.AddSerializer<TSerializer>()))
                            .AddProducer<IOutboxedMessageProducer>(producer =>
                                producer
                                    .WithOutbox()
                                    .DefaultTopic(TopicName)
                                    .AddMiddlewares(m => m.AddSerializer<TSerializer>())
                                )
                            .AddConsumer(consumer =>
                                consumer
                                    .Topic(TopicName)
                                    .WithGroupId($"{prefix}-group-{FixtureId}")
                                    .WithBufferSize(bufferSize)
                                    .WithWorkersCount(workersCount)
                                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                                    .AddMiddlewares(middlewares =>
                                        middlewares
                                            .AddDeserializer<TDeserializer>()
                                            .AddProcessManagers(pm => pm.AddProcessManagersFromAssemblyOf<T>())
                                        )
                            )
                    )
            );

        ServiceProvider = services.BuildServiceProvider();
        ProcessStateStore = (LoggingProcessStateStore)ServiceProvider.GetRequiredService<IProcessStateStore>();
        Producer = ServiceProvider.GetRequiredService<IMessageProducer<IUnOutboxedMessageProducer>>();

        var svc = ServiceProvider.GetServices<IHostedService>();
        foreach (var service in svc)
        {
            service.StartAsync(_fixtureCancellation.Token).GetAwaiter().GetResult();
        }

        _kafkaBus = ServiceProvider.CreateKafkaBus();
        _kafkaBus.StartAsync(_fixtureCancellation.Token).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixtureCancellation.Cancel();
            _fixtureCancellation.Dispose();
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _kafkaBus.StopAsync();
        foreach (var cons in ServiceProvider.GetRequiredService<IConsumerAccessor>().All)
        {
            await cons.StopAsync();
        }
        await ServiceProvider.DisposeAsync();
    }
}

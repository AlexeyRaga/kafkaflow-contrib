using Confluent.Kafka;
using KafkaFlow.Consumers;
using KafkaFlow.Outbox;
using KafkaFlow.Outbox.SqlServer;
using KafkaFlow.ProcessManagers.SqlServer;
using KafkaFlow.Serializer;
using KafkaFlow.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KafkaFlow.ProcessManagers.IntegrationTests.Fixture;

public class SqlServerKafkaFlowFixture : IDisposable, IAsyncDisposable
{
    public readonly string FixtureId = Guid.NewGuid().ToString();
    public string TopicName { get; }
    public readonly ServiceProvider ServiceProvider;
    private readonly IKafkaBus _kafkaBus;
    private readonly CancellationTokenSource _fixtureCancellation;

    public LoggingProcessStateStore ProcessStateStore { get; }

    public IMessageProducer Producer { get; }

    public SqlServerKafkaFlowFixture()
    {
        _fixtureCancellation = new CancellationTokenSource();
        TopicName = $"mssql-messages-{FixtureId}";

        var services = new ServiceCollection();

        var config = new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connStr = config.GetConnectionString("SqlServerBackend");

        services
            .AddSingleton<IConfiguration>(config)
            .AddSingleton(Options.Create(new SqlServerOptions { ConnectionString = connStr }))
            .AddLogging(log => log.AddConsole().AddDebug())
            .AddSqlServerProcessManagerState()
            .Decorate<IProcessStateStore, LoggingProcessStateStore>()
            .AddSqlServerOutboxBackend()
            .AddKafka(kafka =>
                kafka
                    .UseMicrosoftLog()
                    .AddCluster(cluster =>
                        cluster
                            .WithBrokers(new[] { "localhost:9092 " })
                            .CreateTopicIfNotExists(TopicName, 3, 1)
                            .AddOutboxDispatcher(x => x.WithPartitioner(Partitioner.Murmur2Random))
                            .AddProducer<SqlServerKafkaFlowFixture>(producer =>
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
                                    .WithGroupId($"mssql-group-{FixtureId}")
                                    .WithBufferSize(100)
                                    .WithWorkersCount(1)
                                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                                    .AddMiddlewares(middlewares =>
                                        middlewares
                                            .AddDeserializer<JsonCoreDeserializer>()
                                            .AddProcessManagers(pm => pm.AddProcessManagersFromAssemblyOf<SqlServerKafkaFlowFixture>())
                                        )
                            )
                    )
            );
        ServiceProvider = services.BuildServiceProvider();

        ProcessStateStore = (LoggingProcessStateStore)ServiceProvider.GetRequiredService<IProcessStateStore>();

        Producer = ServiceProvider.GetRequiredService<IMessageProducer<SqlServerKafkaFlowFixture>>();

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

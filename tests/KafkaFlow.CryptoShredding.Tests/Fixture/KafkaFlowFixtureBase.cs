using Avro.Util;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaFlow.Consumers;
using KafkaFlow.CryptoShredding.Avro;
using Avro.LogicalTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaFlow.CryptoShredding.Tests.Fixture;

public interface ITestMessageProducer;

public abstract class KafkaFlowFixtureBase : IDisposable, IAsyncDisposable
{
    public readonly string FixtureId = Guid.NewGuid().ToString();
    public string TopicName { get; }

    public readonly IServiceProvider ServiceProvider;
    private readonly CancellationTokenSource _fixtureCancellation = new();

    private readonly IHost _application;

    public IMessageProducer Producer { get; }

    protected abstract void ConfigureHandlers(TypedHandlerConfigurationBuilder builder);

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected KafkaFlowFixtureBase()
    {
        LogicalTypeFactory.Instance.Register(new InlineEncryptedStringLogicalType());
        LogicalTypeFactory.Instance.Register(new EncryptedStringLogicalType());

        TopicName = $"messages-{FixtureId}";

        var host =
            Host
                .CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    ConfigureServices(services);
                    services
                        .AddSingleton<IEncryptionKeyProvider, IdentityEncryptionKeyProvider>()
                        .AddLogging(log => log.AddConsole().AddDebug())
                        .AddKafkaFlowHostedService(kafka =>
                            kafka
                                .UseMicrosoftLog()
                                .AddCluster(cluster =>
                                    cluster
                                        .WithBrokers(["localhost:9092"])
                                        .CreateTopicIfNotExists(TopicName, 1, 1)
                                        .WithSchemaRegistry(x => x.Url = "http://localhost:8081")
                                        .AddProducer<ITestMessageProducer>(producer =>
                                            producer
                                                .DefaultTopic(TopicName)
                                                .AddMiddlewares(middlewares =>
                                                {
                                                    middlewares
                                                        .Add<AvroEncryptionProducerMiddleware>()
                                                        .AddSchemaRegistryAvroSerializer(
                                                            new AvroSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Record });
                                                }))
                                        .AddConsumer(consumer =>
                                            consumer
                                                .Topic(TopicName)
                                                .WithGroupId($"group-{FixtureId}")
                                                .WithBufferSize(5)
                                                .WithWorkersCount(1)
                                                .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                                                .AddMiddlewares(middlewares =>
                                                    middlewares
                                                        .AddSchemaRegistryAvroDeserializer()
                                                        .Add<AvroEncryptionConsumerMiddleware>()
                                                        .AddTypedHandlers(ConfigureHandlers)
                                                )
                                        )
                                )
                        );
                });

        _application = host.Build();
        ServiceProvider = _application.Services;

        Producer = ServiceProvider.GetRequiredService<IMessageProducer<ITestMessageProducer>>();

        _application.StartAsync(_fixtureCancellation.Token).GetAwaiter().GetResult();
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
            DisposeAsyncCore().GetAwaiter().GetResult();
        }
    }

    private async ValueTask DisposeAsyncCore()
    {
        await _application.StopAsync();
        await Task.WhenAll(
            ServiceProvider
                .GetRequiredService<IConsumerAccessor>().All
                .Select(x => x.StopAsync()));
    }
}

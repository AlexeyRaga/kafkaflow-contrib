# KafkaFlow - extra libraries

This project contains a set of libraries that contribute to [KafkaFlow](https://github.com/Farfetch/kafkaflow)
ecosystem.

- [Contrib.KafkaFlow.Outbox](./src/Contrib.KafkaFlow.Outbox/Readme.md)

  is a library to provide [transactional outbox](https://microservices.io/patterns/data/transactional-outbox.html)
  for KafkaFlow.

  The following backends are implemented:

  - [Contrib.KafkaFlow.Outbox.Postgres](./src/Contrib.KafkaFlow.Outbox.Postgres) - Postgres SQL backend


- [Contrib.KafkaFlow.ProcessManagers](./src/Contrib.KafkaFlow.ProcessManagers/Readme.md)

  is a library that provides [Process Managers](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html)
  functionality (sometimes also called [Sagas](./src/Contrib.KafkaFlow.ProcessManagers/docs/pm-or-saga.md)).

  The following backends are implemented:

  - [Contrib.KafkaFlow.ProcessManagers.Postgres](./src/Contrib.KafkaFlow.ProcessManagers.Postgres) -
    Postgres SQL backend for storing process' state

## Usage example

As a pattern, [Process Managers](./src/Contrib.KafkaFlow.ProcessManagers/Readme.md)
requires that the state _cannot_ be an eventually-consistent projection, and must be immediately consistent.
It also requires that any messages that are published _must be_ transactionally consistent with the state changes.

It means that using process managers implies using [Outbox](./src/Contrib.KafkaFlow.Outbox/Readme.md) pattern.

Here is how process managers and outbox can be used together:

```csharp
services
    // We need an NpgsqlDataSource shared between Outbox and Process Managers
    // to be able to update state and send messages transactionally
    .AddSingleton(myNpgsqlDataSource)
    .AddPostgresProcessManagerState()
    .AddPostgresOutboxBackend()
    .AddKafka(kafka =>
        kafka
            .AddCluster(cluster =>
                cluster
                    // The dispatcher service will be started in background
                    .AddOutboxDispatcher(dispatcher =>
                        // I strongly recommend to use Murmur2Random since it is
                        // the "original" default in Java ecosystem, and is shared by other
                        // ecosystems such as JavaScript or Python.
                        dispatcher.WithPartitioner(Partitioner.Murmur2Random))
                    .AddProducer("default", producer =>
                        producer
                            // Make this producer go through the outbox
                            .WithOutbox()
                            .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>()))
    // and so on
```


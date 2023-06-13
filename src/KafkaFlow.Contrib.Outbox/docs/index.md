# Transactional Outbox

A [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) implementation for MSSQL and Postgres

## Overview

The implementation of `Transactional Outbox` pattern for KafkaFlow.

The idea of this library is to be able to "wrap" a producer with an Outbox decorator.
This way all the existing producer middlewares (serialisation, etc.) would apply,
but the messages would be sent to the Outbox instead of being pushed to Kafka immediately
upon publishing.

Here is how Outbox can be enabled when configuring a KafkaFlow producer:

```csharp
services
    .AddInMemoryOutboxBackend() // configure the backend
    .AddKafka(kafka =>
        kafka.AddCluster(cluster =>
            cluster
                .AddProducer<IMyProducer>(producer =>
                    producer
                        .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>())
                        .WithOutbox())
```

# KafkaFlow MongoDB Outbox

A MongoDB implementation of the [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) for KafkaFlow.

## Overview

This package provides MongoDB-based outbox functionality that ensures reliable message publishing by storing outbox records in MongoDB
collections and dispatching them transactionally.
It guarantees that your business data changes and Kafka message publishing happen atomically.

## Key Features

- **Transactional Consistency**: Ensures atomicity between business operations and message publishing
- **MongoDB Native**: Built specifically for MongoDB with proper session and transaction support
- **Distributed Locking**: Prevents multiple instances from processing the same outbox records
- **Automatic Sequencing**: Maintains message ordering through sequence IDs
- **Nested Scope Support**: Handles complex transaction scenarios with proper ownership management

## Prerequisites

- **MongoDB Replica Set or Sharded Cluster**: MongoDB transactions require a replica set or sharded cluster configuration
- **Single Instance Limitation**: While the outbox will work with a standalone MongoDB instance, transactions are not supported,
    and data consistency cannot be guaranteed. This configuration doesn't make much sense for the outbox pattern
    as it defeats the purpose of transactional guarantees.

## Installation

```bash
dotnet add package Contrib.KafkaFlow.Outbox.MongoDb
```

## Configuration

### Basic Setup

```csharp
services
    .AddMongoDbOutboxBackend(mongoDatabase, "my_outbox")
    .AddKafka(kafka =>
        kafka.AddCluster(cluster =>
            cluster
                .AddOutboxDispatcher()
                .AddProducer<IMyProducer>(producer =>
                    producer
                        .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>())
                        .WithOutbox()))); // Enable outbox for this producer
```

## Usage

### Required: Transaction Scope Management

**Important**: All outbox operations must be wrapped in a `MongoDbTransactionScope`.
This is mandatory and enforced by the repository - operations will throw `MongoDbTransactionScopeRequiredException`
if no active scope is detected.

### Basic Usage

```csharp
public class OrderService
{
    private readonly IMongoDatabase _database;
    private readonly IOutboxProducer _outboxProducer;

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // Always wrap outbox operations in a transaction scope
        using var scope = MongoDbTransactionScope.Create(_database.Client);

        // Perform your business operations
        var order = new Order(request.CustomerId, request.Items);
        await _ordersCollection.InsertOneAsync(scope.CurrentSession, order);

        // Publish event through outbox
        await _outboxProducer.ProduceAsync(
            "order-events",
            order.Id.ToString(),
            new OrderCreatedEvent(order.Id, order.CustomerId, order.Total)
        );

        // Commit the transaction
        scope.Complete();
    }
}
```

### Advanced: Nested Scopes

The transaction scope supports nesting with proper ownership management:

```csharp
public async Task ComplexBusinessOperation()
{
    // Outer scope - owns the session
    using var outerScope = MongoDbTransactionScope.Create(_mongoClient);

    await PerformBusinessLogic();

    // Inner scope - reuses existing session without ownership
    await CallAnotherServiceMethod(); // This method can create its own scope

    outerScope.Complete(); // Only the owning scope manages transaction commit
}

private async Task CallAnotherServiceMethod()
{
    // This scope reuses the existing session and doesn't manage transactions
    using var innerScope = MongoDbTransactionScope.Create(_mongoClient);

    await _outboxProducer.ProduceAsync("events", "key", eventData);

    innerScope.Complete(); // No-op for non-owning scopes
}
```

## Session Management

### Checking Session Availability

```csharp
// Check if a session is available
if (MongoDbTransactionScope.HasSession)
{
    // Session is available for operations
}

// Safely get the current session
if (MongoDbTransactionScope.TryGetSession(out var session))
{
    // Use session for MongoDB operations
    await collection.InsertOneAsync(session, document);
}
```

### Transaction Support Detection

```csharp
using var scope = MongoDbTransactionScope.Create(_mongoClient);

if (scope.SupportsTransactions)
{
    // Full transactional support available
    Console.WriteLine("Running with transaction support");
}
else
{
    // Standalone MongoDB - no transactions
    Console.WriteLine("Running without transaction support");
}
```

## Collections and Indexes

The outbox automatically creates the following collections:

- **Main Collection** (default: `outbox`): Stores outbox records
- **Lock Collection** (default: `outbox_locks`): Manages distributed locking

Indexes are automatically created for optimal performance:
- Ascending index on `SequenceId` for efficient ordering

## Best Practices

### 1. Always Use Transaction Scopes
```csharp
// ✅ Correct
using var scope = MongoDbTransactionScope.Create(mongoClient);
await _outboxProducer.ProduceAsync("topic", "key", data);
scope.Complete();

// ❌ Incorrect - will throw exception
await _outboxProducer.ProduceAsync("topic", "key", data);
```

### 2. Handle Nested Operations Properly
```csharp
// ✅ Correct - create scope at the highest level
using var scope = MongoDbTransactionScope.Create(mongoClient);
await BusinessOperation1();
await BusinessOperation2(); // These can create their own scopes
scope.Complete();
```

## Troubleshooting

### Common Exceptions

**`MongoDbTransactionScopeRequiredException`**
- **Cause**: Attempting to use outbox operations without an active transaction scope
- **Solution**: Wrap operations in `MongoDbTransactionScope.Create(mongoClient)`

### Performance Considerations

- **Batch Size**: Configure appropriate batch sizes for outbox dispatching. Processing a batch must be doable withing the lock duration.
- **Lock Duration**: Is hardcoded to be 10 seconds. Not configurable for now.

## Related Documentation

- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [MongoDB Transactions](https://docs.mongodb.com/manual/core/transactions/)
- [KafkaFlow Documentation](https://github.com/Farfetch/kafkaflow)

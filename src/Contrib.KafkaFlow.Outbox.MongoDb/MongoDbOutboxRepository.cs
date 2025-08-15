using KafkaFlow.Outbox;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

public class MongoDbOutboxRepository : IOutboxRepository
{
    private readonly IMongoCollection<OutboxDocument> _collection;
    private readonly IMongoCollection<LockDocument> _lockCollection;
    private readonly IMongoClient _client;
    private readonly string _instanceId;
    private long _sequenceCounter;

    public MongoDbOutboxRepository(IMongoDatabase database, string collectionName = "outbox")
    {
        _collection = database.GetCollection<OutboxDocument>(collectionName);
        _lockCollection = database.GetCollection<LockDocument>($"{collectionName}_locks");
        _client = database.Client;
        _instanceId = Environment.MachineName + "-" + Environment.ProcessId + "-" + Guid.NewGuid().ToString("N")[..8];

        var indexKeys = Builders<OutboxDocument>.IndexKeys.Ascending(x => x.SequenceId);
        var indexModel = new CreateIndexModel<OutboxDocument>(indexKeys);
        _collection.Indexes.CreateOne(indexModel);

        InitializeSequenceCounter();
    }

    private void InitializeSequenceCounter()
    {
        var lastDocument = _collection.Find(Builders<OutboxDocument>.Filter.Empty)
            .Sort(Builders<OutboxDocument>.Sort.Descending(x => x.SequenceId))
            .FirstOrDefault();

        _sequenceCounter = lastDocument?.SequenceId ?? 0;
    }

    public async ValueTask Store(OutboxTableRow outboxTableRow, CancellationToken token = default)
    {
        // We require a transaction scope to be opened explicitly by the user before
        // we allow using the outbox repository.
        if (!MongoDbTransactionScope.TryGetSession(out var session))
            throw MongoDbTransactionScopeRequiredException.ForMissingScope();

        var id = ObjectId.GenerateNewId();
        var sequenceId = Interlocked.Increment(ref _sequenceCounter);

        var document = new OutboxDocument
        {
            Id = id,
            SequenceId = sequenceId,
            TopicName = outboxTableRow.TopicName,
            Partition = outboxTableRow.Partition,
            MessageKey = outboxTableRow.MessageKey,
            MessageHeaders = outboxTableRow.MessageHeaders,
            MessageBody = outboxTableRow.MessageBody,
            CreatedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(session, document, cancellationToken: token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<OutboxTableRow>> Read(int batchSize, CancellationToken token = default)
    {
        // We require a transaction scope to be opened explicitly by the user before
        // we allow using the outbox repository.
        // In case of reading and dispatching outbox messages, we rely on a Dispatcher to
        // manage the transaction scope.
        if (!MongoDbTransactionScope.TryGetSession(out var session))
            throw MongoDbTransactionScopeRequiredException.ForMissingScope();

        // Try to acquire the dispatcher lock
        // For now let's assume that 10 seconds is a reasonable lock duration to be able to process a batch
        var lockAcquired = await TryAcquireLock("outbox_dispatcher", TimeSpan.FromSeconds(10), token);
        if (!lockAcquired)
        {
            // Couldn't acquire lock - another instance is dispatching
            return [];
        }

        var filter = Builders<OutboxDocument>.Filter.Empty;
        var sort = Builders<OutboxDocument>.Sort.Ascending(x => x.SequenceId);

        var documents = await _collection.Find(session, filter)
            .Sort(sort)
            .Limit(batchSize)
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (documents.Count == 0) return [];

        var documentIds = documents.Select(d => d.Id).ToList();
        var deleteFilter = Builders<OutboxDocument>.Filter.In(x => x.Id, documentIds);

        await _collection.DeleteManyAsync(session, deleteFilter, cancellationToken: token).ConfigureAwait(false);

        var rows = documents.Select(CreateOutboxTableRow).ToList();

        return rows;
    }

    private async Task<bool> TryAcquireLock(string lockName, TimeSpan lockDuration, CancellationToken token = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(lockDuration);

        var filter = Builders<LockDocument>.Filter.And(
            Builders<LockDocument>.Filter.Eq(x => x.Id, lockName),
            Builders<LockDocument>.Filter.Or(
                Builders<LockDocument>.Filter.Eq(x => x.LockOwner, null),        // No current lock
                Builders<LockDocument>.Filter.Lt(x => x.ExpiresAt, now)          // Expired lock
            )
        );

        var update = Builders<LockDocument>.Update
            .Set(x => x.LockOwner, _instanceId)
            .Set(x => x.ExpiresAt, expiresAt);

        var options = new FindOneAndUpdateOptions<LockDocument>
        {
            IsUpsert = true,                    // Create if doesn't exist
            ReturnDocument = ReturnDocument.After
        };

        try
        {
            var result = await _lockCollection.FindOneAndUpdateAsync(filter, update, options, token);

            // We got the lock if the returned document has our instance ID
            return result?.LockOwner == _instanceId;
        }
        catch (MongoException)
        {
            // Lock contention or other MongoDB error
            return false;
        }
    }

    public ITransactionScope BeginTransaction() =>
        MongoDbTransactionScope.Create(_client);

    private static OutboxTableRow CreateOutboxTableRow(OutboxDocument doc) =>
        new
        (
            doc.SequenceId,
            doc.TopicName,
            doc.Partition,
            doc.MessageKey,
            doc.MessageHeaders,
            doc.MessageBody
        );
}

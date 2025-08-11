using KafkaFlow.Outbox;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

public class MongoDbOutboxRepository : IOutboxRepository
{
    private readonly IMongoCollection<OutboxDocument> _collection;
    private readonly IMongoClient _client;
    private long _sequenceCounter;

    public MongoDbOutboxRepository(IMongoDatabase database, string collectionName = "outbox")
    {
        _collection = database.GetCollection<OutboxDocument>(collectionName);
        _client = database.Client;

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

        await _collection.InsertOneAsync(document, cancellationToken: token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<OutboxTableRow>> Read(int batchSize, CancellationToken token = default)
    {
        var filter = Builders<OutboxDocument>.Filter.Empty;
        var sort = Builders<OutboxDocument>.Sort.Ascending(x => x.SequenceId);

        var documents = await _collection.Find(filter)
            .Sort(sort)
            .Limit(batchSize)
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (documents.Count == 0)
            return [];

        var documentIds = documents.Select(d => d.Id).ToList();
        var deleteFilter = Builders<OutboxDocument>.Filter.In(x => x.Id, documentIds);

        await _collection.DeleteManyAsync(deleteFilter, token).ConfigureAwait(false);

        return documents.Select(CreateOutboxTableRow);
    }

    public ITransactionScope BeginTransaction()
    {
        return new MongoDbTransactionScope(_client);
    }

    private static OutboxTableRow CreateOutboxTableRow(OutboxDocument doc)
    {
        return new
        (
            doc.SequenceId,
            doc.TopicName,
            doc.Partition,
            doc.MessageKey,
            doc.MessageHeaders,
            doc.MessageBody
        );
    }
}

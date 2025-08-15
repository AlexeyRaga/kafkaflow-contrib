using Contrib.KafkaFlow.Outbox.MongoDb;
using KafkaFlow.Outbox;
using KafkaFlow.ProcessManagers;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.ProcessManagers.MongoDb;

public sealed class MongoDbProcessStateRepository : IProcessStateRepository
{
    private readonly IMongoCollection<ProcessStateDocument> _collection;
    private readonly IMongoDatabase _database;

    public MongoDbProcessStateRepository(IMongoDatabase database, string collectionName = "process_states")
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _collection = database.GetCollection<ProcessStateDocument>(collectionName);

        var indexKeysDefinition = Builders<ProcessStateDocument>.IndexKeys
            .Ascending(x => x.ProcessType)
            .Ascending(x => x.ProcessId);

        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<ProcessStateDocument>(indexKeysDefinition, indexOptions);

        _collection.Indexes.CreateOne(indexModel);
    }

    private static Guid GenerateProcessId(Type processType, Guid processId) =>
        GuidUtils.GenerateV5(processType.FullName!, processId);

    public async ValueTask<int> Persist(Type processType, string processState, Guid processId, VersionedState state)
    {
        if (!MongoDbTransactionScope.TryGetSession(out var session))
            throw MongoDbTransactionScopeRequiredException.ForMissingScope();

        var id = GenerateProcessId(processType, processId);

        var document = new ProcessStateDocument
        {
            Id = id,
            ProcessType = processType.FullName!,
            ProcessId = processId,
            ProcessState = processState,
            Version = state.Version,
            DateUpdatedUtc = DateTime.UtcNow
        };

        if (state.Version == 0)
        {
            try
            {
                await _collection.InsertOneAsync(session, document).ConfigureAwait(false);
                return 1;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return 0;
            }
        }

        var updateFilter = Builders<ProcessStateDocument>.Filter.And(
            Builders<ProcessStateDocument>.Filter.Eq(x => x.Id, id),
            Builders<ProcessStateDocument>.Filter.Eq(x => x.Version, state.Version));

        var update = Builders<ProcessStateDocument>.Update
            .Set(x => x.ProcessState, processState)
            .Set(x => x.Version, state.Version + 1)
            .Set(x => x.DateUpdatedUtc, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(session, updateFilter, update).ConfigureAwait(false);

        return (int)result.ModifiedCount;
    }

    public async ValueTask<IEnumerable<ProcessStateTableRow>> Load(Type processType, Guid processId)
    {
        var id = GenerateProcessId(processType, processId);
        var filter = Builders<ProcessStateDocument>.Filter.Eq(x => x.Id, id);

        var documents = await _collection.Find(filter).ToListAsync().ConfigureAwait(false);

        return documents.Select(doc => new ProcessStateTableRow(doc.ProcessState, doc.Version));
    }

    public async ValueTask<int> Delete(Type processType, Guid processId, int version)
    {
        if (!MongoDbTransactionScope.TryGetSession(out var session))
            throw MongoDbTransactionScopeRequiredException.ForMissingScope();

        var id = GenerateProcessId(processType, processId);
        var filter = Builders<ProcessStateDocument>.Filter.And(
            Builders<ProcessStateDocument>.Filter.Eq(x => x.Id, id),
            Builders<ProcessStateDocument>.Filter.Eq(x => x.Version, version));

        var result = await _collection.DeleteOneAsync(session, filter).ConfigureAwait(false);

        return (int)result.DeletedCount;
    }

    public ITransactionScope CreateTransactionScope(TimeSpan timeout) =>
        MongoDbTransactionScope.Create(_database.Client, timeout);
}

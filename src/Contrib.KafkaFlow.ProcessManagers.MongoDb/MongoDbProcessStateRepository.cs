using KafkaFlow.ProcessManagers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.ProcessManagers.MongoDb;

public sealed class MongoDbProcessStateRepository : IProcessStateRepository
{
    private readonly IMongoCollection<ProcessStateDocument> _collection;

    public MongoDbProcessStateRepository(IMongoDatabase database, string collectionName = "process_states")
    {
        _collection = database.GetCollection<ProcessStateDocument>(collectionName);

        var indexKeysDefinition = Builders<ProcessStateDocument>.IndexKeys
            .Ascending(x => x.ProcessType)
            .Ascending(x => x.ProcessId);

        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<ProcessStateDocument>(indexKeysDefinition, indexOptions);

        _collection.Indexes.CreateOneAsync(indexModel);
    }

    public async ValueTask<int> Persist(Type processType, string processState, Guid processId, VersionedState state)
    {
        var processObjectId = ObjectId.Parse(processId.ToString("N")[..24]); // Convert Guid to ObjectId

        var filter = Builders<ProcessStateDocument>.Filter.And(
            Builders<ProcessStateDocument>.Filter.Eq(x => x.ProcessType, processType.FullName),
            Builders<ProcessStateDocument>.Filter.Eq(x => x.ProcessId, processObjectId));

        var id = ObjectId.GenerateNewId();

        var document = new ProcessStateDocument
        {
            Id = id,
            ProcessType = processType.FullName!,
            ProcessId = processObjectId,
            ProcessState = processState,
            Version = state.Version,
            DateUpdatedUtc = DateTime.UtcNow
        };

        if (state.Version == 0)
        {
            try
            {
                await _collection.InsertOneAsync(document).ConfigureAwait(false);
                return 1;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return 0;
            }
        }

        var updateFilter = Builders<ProcessStateDocument>.Filter.And(
            filter,
            Builders<ProcessStateDocument>.Filter.Eq(x => x.Version, state.Version));

        var update = Builders<ProcessStateDocument>.Update
            .Set(x => x.ProcessState, processState)
            .Set(x => x.Version, state.Version + 1)
            .Set(x => x.DateUpdatedUtc, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(updateFilter, update).ConfigureAwait(false);

        return (int)result.ModifiedCount;
    }

    public async ValueTask<IEnumerable<ProcessStateTableRow>> Load(Type processType, Guid processId)
    {
        var processObjectId = ObjectId.Parse(processId.ToString("N")[..24]); // Convert Guid to ObjectId

        var filter = Builders<ProcessStateDocument>.Filter.And(
            Builders<ProcessStateDocument>.Filter.Eq(x => x.ProcessType, processType.FullName),
            Builders<ProcessStateDocument>.Filter.Eq(x => x.ProcessId, processObjectId));

        var documents = await _collection.Find(filter).ToListAsync().ConfigureAwait(false);

        return documents.Select(doc => new ProcessStateTableRow(doc.ProcessState, doc.Version));
    }

    public async ValueTask<int> Delete(Type processType, Guid processId, int version)
    {
        var processObjectId = ObjectId.Parse(processId.ToString("N")[..24]); // Convert Guid to ObjectId

        var filter = Builders<ProcessStateDocument>.Filter.And(
            Builders<ProcessStateDocument>.Filter.Eq(x => x.ProcessType, processType.FullName),
            Builders<ProcessStateDocument>.Filter.Eq(x => x.ProcessId, processObjectId),
            Builders<ProcessStateDocument>.Filter.Eq(x => x.Version, version));

        var result = await _collection.DeleteOneAsync(filter).ConfigureAwait(false);

        return (int)result.DeletedCount;
    }
}

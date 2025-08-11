using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Contrib.KafkaFlow.ProcessManagers.MongoDb;

public sealed class ProcessStateDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("processType")]
    public required string ProcessType { get; set; }

    [BsonElement("processId")]
    public ObjectId ProcessId { get; set; }

    [BsonElement("processState")]
    public required string ProcessState { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("dateUpdatedUtc")]
    public DateTime DateUpdatedUtc { get; set; }
}

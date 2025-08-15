using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Contrib.KafkaFlow.ProcessManagers.MongoDb;

public sealed class ProcessStateDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonElement("processType")]
    public required string ProcessType { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [BsonElement("processId")]
    public Guid ProcessId { get; set; }

    [BsonElement("processState")]
    public required string ProcessState { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("dateUpdatedUtc")]
    public DateTime DateUpdatedUtc { get; set; }
}

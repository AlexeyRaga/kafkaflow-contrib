using MongoDB.Bson.Serialization.Attributes;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

public sealed class LockDocument
{
    [BsonId]
    public required string Id { get; set; }

    [BsonElement("lockOwner")]
    public string? LockOwner { get; set; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

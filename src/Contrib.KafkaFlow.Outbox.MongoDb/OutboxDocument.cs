using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

public class OutboxDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("sequenceId")]
    public long SequenceId { get; set; }

    [BsonElement("topicName")]
    public required string TopicName { get; set; }

    [BsonElement("partition")]
    public int? Partition { get; set; }

    [BsonElement("messageKey")]
    public byte[]? MessageKey { get; set; }

    [BsonElement("messageHeaders")]
    public string? MessageHeaders { get; set; }

    [BsonElement("messageBody")]
    public byte[]? MessageBody { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

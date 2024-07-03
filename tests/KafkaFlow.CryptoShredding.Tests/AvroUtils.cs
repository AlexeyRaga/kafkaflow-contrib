using Avro.IO;
using Avro.Specific;

namespace KafkaFlow.CryptoShredding.Tests;

public static class AvroUtils
{
    public static string SerializeToJson(this ISpecificRecord record)
    {
        var schema = record.Schema;
        using var stream = new MemoryStream();
        var writer = new SpecificDatumWriter<ISpecificRecord>(schema);
        var jsonEncoder = new JsonEncoder(schema, stream, true);

        writer.Write(record, jsonEncoder);
        jsonEncoder.Flush();

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static T DeserializeFromJson<T>(string json) where T : ISpecificRecord, new()
    {
        var schema = new T().Schema;
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);

        writer.Write(json);
        writer.Flush();

        stream.Seek(0, SeekOrigin.Begin);
        var jsonDecoder = new JsonDecoder(schema, stream);
        var reader = new SpecificDatumReader<T>(schema, schema);

        var result = new T();
        reader.Read(result, jsonDecoder);
        return result;
    }

    public static byte[] SerializeToBinary(this ISpecificRecord record)
    {
        using var stream = new MemoryStream();
        var schema = record.Schema;
        var writer = new SpecificDatumWriter<ISpecificRecord>(schema);
        var encoder = new BinaryEncoder(stream);

        writer.Write(record, encoder);
        return stream.ToArray();
    }

    public static T DeserializeFromBinary<T>(byte[] data) where T : ISpecificRecord, new()
    {
        using var stream = new MemoryStream(data);
        var schema = new T().Schema;
        var decoder = new BinaryDecoder(stream);
        var reader = new SpecificDatumReader<T>(schema, schema);

        var result = new T();
        reader.Read(result, decoder);
        return result;
    }
}

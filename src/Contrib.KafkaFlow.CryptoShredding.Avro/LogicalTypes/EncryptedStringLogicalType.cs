using Avro.Util;

namespace Avro.LogicalTypes;

public abstract record EncryptedString
{
    private EncryptedString()
    {
    }

    public sealed record Encrypted(string Value) : EncryptedString;

    public sealed record Plain(string Value) : EncryptedString;

    public static EncryptedString FromPlain(string value) => new Plain(value);
    public static EncryptedString FromEncrypted(string value) => new Encrypted(value);

    public bool IsEncrypted => this is Encrypted;
}

public sealed class EncryptedStringLogicalType() : LogicalType(TypeName)
{
    public const string TypeName = "encrypted-string";

    public override object ConvertToBaseValue(object logicalValue, LogicalSchema schema) =>
        logicalValue switch
        {
            EncryptedString.Encrypted e => e.Value,
            _ => throw new AvroTypeException("Only encrypted values can be serialised")
        };

    public override object ConvertToLogicalValue(object baseValue, LogicalSchema schema) =>
        new EncryptedString.Encrypted((string)baseValue);

    public override Type GetCSharpType(bool nullible) => typeof(EncryptedString);

    public override bool IsInstanceOfLogicalType(object logicalValue) => logicalValue is EncryptedString;

    public override void ValidateSchema(LogicalSchema schema)
    {
        if (Schema.Type.String != schema.BaseSchema.Tag)
            throw new AvroTypeException("'encrypted-string' can only be used with an underlying string type");
    }
}

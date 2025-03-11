using System.Collections;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Avro;
using Avro.Specific;
using Avro.LogicalTypes;

namespace KafkaFlow.CryptoShredding.Avro;

internal enum EncryptionMode
{
    Encrypt,
    Decrypt
}

public static class AvroCryptoShredder
{
    private static readonly ConcurrentDictionary<string, List<int>> SchemaCache = new();

    public static void Encrypt<T>(IKeyedEncryptor encryptor, T record) where T : ISpecificRecord =>
        TransformRecursively(record.Schema, record, EncryptionMode.Encrypt, encryptor.Encrypt);

    public static void Encrypt(IKeyedEncryptor encryptor, ISpecificRecord record) =>
        TransformRecursively(record.Schema, record, EncryptionMode.Encrypt, encryptor.Encrypt);

    public static void Decrypt(IKeyedEncryptor encryptor, ISpecificRecord record)
    {
        var decryptor = (string value) =>
        {
            try
            {
                return encryptor.Decrypt(value);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return value;
            }
        };
        TransformRecursively(record.Schema, record, EncryptionMode.Decrypt, decryptor);
    }

    public static void Decrypt<T>(IKeyedEncryptor encryptor, T record) where T : ISpecificRecord =>
        Decrypt(encryptor, (ISpecificRecord)record);

    private static object? TransformRecursively(Schema schema, object? value, EncryptionMode mode, Func<string, string> transform)
    {
        if (value == null) return null;
        if (TransformValue(schema, value, mode, transform, out var newValue)) return newValue;

        var recurse = (Schema s, object? v) => TransformRecursively(s, v, mode, transform);

        switch (schema)
        {
            case RecordSchema s:
                var indices = GetEncryptedStringIndexes(s);
                var fields = s.Fields.FindAll(x => indices.Contains(x.Pos));
                if (value is not ISpecificRecord record) return value;

                foreach (var field in fields)
                {
                    var fldValue = record.Get(field.Pos);
                    var newFldValue = recurse(field.Schema, fldValue);
                    record.Put(field.Pos, newFldValue);
                }

                return record;

            // micro optimisation - do we even need it?
            case ArraySchema s when IsEncryptedPlainStringSchema(s.ItemSchema):
                return ((IList<string>)value).Select(transform).ToList();

            case ArraySchema s when IsTypedEncryptedStringSchema(s.ItemSchema):
                return ((IList<EncryptedString>)value).Select(x => recurse(s.ItemSchema, x)).Cast<EncryptedString>().ToList();

            case ArraySchema { ItemSchema: RecordSchema } s:
                //employ the fact that records are mutable, don't need to re-create a list
                foreach (var item in (IEnumerable)value) recurse(s.ItemSchema, item);
                return value;

            case ArraySchema s:
                return ReflectionUtils.MapFunctionToList(value, x => recurse(s.ItemSchema, x));

            // micro optimisation - do we even need it?
            case MapSchema s when IsEncryptedPlainStringSchema(s.ValueSchema):
                return ((IDictionary<string, string>)value).ToDictionary(x => x.Key, x => transform(x.Value));

            case MapSchema { ValueSchema: RecordSchema } s:
                //employ the fact that records are mutable, don't need to re-create a list
                foreach (var item in ((IDictionary)value).Values) recurse(s.ValueSchema, item);
                return value;

            case MapSchema s:
                return ReflectionUtils.MapFunctionToDictionary(value, x => recurse(s.ValueSchema, x));

            case UnionSchema s:
                switch (value)
                {
                    case ISpecificRecord v:
                        return recurse(v.Schema, v);
                    case string v when s.Schemas.Any(IsEncryptedPlainStringSchema):
                        return transform(v);
                    case EncryptedString v when s.Schemas.Any(IsTypedEncryptedStringSchema):
                        return recurse(s.Schemas.First(IsTypedEncryptedStringSchema), v);
                    case IDictionary v:
                        var valueSchema = s.Schemas.FirstOrDefault(x => x.Tag == Schema.Type.Map);
                        return valueSchema is null ? v : recurse(valueSchema, v);
                    case IList v:
                        var itemSchema = s.Schemas.FirstOrDefault(x => x.Tag == Schema.Type.Array);
                        return itemSchema is null ? v : recurse(itemSchema, v);
                    default:
                        return value;
                }
        }

        return value;
    }

    private static bool TransformValue(
        Schema schema,
        object? value,
        EncryptionMode mode,
        Func<string, string> transform,
        out object? transformedValue)
    {
        transformedValue = null;
        if (IsEncryptedPlainStringSchema(schema))
        {
            if (value is not string plainValue) return false;
            transformedValue = transform(plainValue);
            return true;
        }

        if (IsTypedEncryptedStringSchema(schema))
        {
            switch (mode, value)
            {
                case (EncryptionMode.Encrypt, EncryptedString.Plain x):
                    transformedValue = new EncryptedString.Encrypted(transform(x.Value));
                    return true;
                case (EncryptionMode.Decrypt, EncryptedString.Encrypted x):
                    transformedValue = new EncryptedString.Plain(transform(x.Value));
                    return true;
                default: return false;
            }
        }

        return false;
    }


    private static List<int> GetEncryptedStringIndexes(RecordSchema schema)
    {
        var schemaKey = schema.Fullname;
        if (SchemaCache.TryGetValue(schemaKey, out var indexes)) return indexes;
        return DiscoverEncryptedFields(schema) ? SchemaCache[schemaKey] : [];
    }

    private static bool DiscoverEncryptedFields(Schema schema) =>
        schema switch
        {
            RecordSchema s => HandleRecordSchema(s),
            UnionSchema s => s.Schemas.Select(DiscoverEncryptedFields).Aggregate((a, b) => a || b),
            ArraySchema s => DiscoverEncryptedFields(s.ItemSchema),
            MapSchema s => DiscoverEncryptedFields(s.ValueSchema),
            _ => IsEncryptedPlainStringSchema(schema) || IsTypedEncryptedStringSchema(schema)
        };

    private static bool HandleRecordSchema(RecordSchema recordSchema)
    {
        if (SchemaCache.ContainsKey(recordSchema.Fullname)) return true;

        var fieldsWithEncryptedString = recordSchema.Fields
            .Where(field => DiscoverEncryptedFields(field.Schema))
            .Select(field => field.Pos)
            .ToList();

        SchemaCache.TryAdd(recordSchema.Fullname, fieldsWithEncryptedString);
        return fieldsWithEncryptedString.Count != 0;
    }

    private static bool IsEncryptedPlainStringSchema(Schema schema) =>
        schema is LogicalSchema { LogicalTypeName: InlineEncryptedStringLogicalType.TypeName };

    private static bool IsTypedEncryptedStringSchema(Schema schema) =>
        schema is LogicalSchema { LogicalTypeName: EncryptedStringLogicalType.TypeName };
}

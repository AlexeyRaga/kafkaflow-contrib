namespace KafkaFlow.Outbox.SqlServer;

/// <summary>
/// Locates the outbox table. Defaults match the shipped schema scripts.
/// </summary>
public sealed record SqlServerOutboxOptions
{
    private readonly string _schemaName = "outbox";
    private readonly string _tableName = "outbox";

    public string SchemaName
    {
        get => _schemaName;
        init => _schemaName = NotBlank(value, nameof(SchemaName));
    }

    public string TableName
    {
        get => _tableName;
        init => _tableName = NotBlank(value, nameof(TableName));
    }

    public string QualifiedTableName => $"{Quote(_schemaName)}.{Quote(_tableName)}";

    // Rejected on the way in, not in QualifiedTableName: the record's synthesised ToString reads that
    // property, so validating there would make logging a misconfigured instance throw.
    private static string NotBlank(string identifier, string propertyName) =>
        string.IsNullOrWhiteSpace(identifier)
            ? throw new ArgumentException("Identifier must not be empty", propertyName)
            : identifier;

    // QUOTENAME equivalent: identifiers reach the SQL as text, so a stray ] must not end the quoting.
    private static string Quote(string identifier) =>
        $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}

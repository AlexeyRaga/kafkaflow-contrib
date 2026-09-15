namespace KafkaFlow.Outbox.SqlServer;

/// <summary>
/// Locates the outbox table. Defaults match the shipped schema scripts.
/// </summary>
public sealed record SqlServerOutboxOptions
{
    public string SchemaName { get; init; } = "outbox";

    public string TableName { get; init; } = "outbox";

    public string QualifiedTableName =>
        $"{Quote(SchemaName, nameof(SchemaName))}.{Quote(TableName, nameof(TableName))}";

    // QUOTENAME equivalent: identifiers reach the SQL as text, so a stray ] must not end the quoting.
    private static string Quote(string identifier, string propertyName) =>
        string.IsNullOrWhiteSpace(identifier)
            ? throw new ArgumentException("Identifier must not be empty", propertyName)
            : $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}

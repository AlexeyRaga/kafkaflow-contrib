namespace Contrib.KafkaFlow.Outbox.MongoDb;

/// <summary>
/// The exception that is thrown when an attempt is made to use MongoDB outbox operations
/// without an active <see cref="MongoDbTransactionScope"/>.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by MongoDB outbox repository operations when no transaction scope
/// is available in the current execution context. MongoDB outbox operations require an active
/// transaction scope to ensure data consistency and proper transaction management.
/// </para>
/// <para>
/// To resolve this exception, wrap your outbox operations within a <see cref="MongoDbTransactionScope"/>:
/// </para>
/// <code>
/// using var scope = MongoDbTransactionScope.Create(mongoClient);
/// // Perform outbox operations here
/// scope.Complete();
/// </code>
/// </remarks>
public sealed class MongoDbTransactionScopeRequiredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbTransactionScopeRequiredException"/> class
    /// with a default error message.
    /// </summary>
    public MongoDbTransactionScopeRequiredException()
        : base("MongoDB outbox operations require an active MongoDbTransactionScope. " +
               "Please wrap your operations within a MongoDbTransactionScope using " +
               "MongoDbTransactionScope.Create(mongoClient).")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbTransactionScopeRequiredException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    internal MongoDbTransactionScopeRequiredException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="MongoDbTransactionScopeRequiredException"/> with a message
    /// that includes guidance for nested scope scenarios.
    /// </summary>
    /// <returns>A new <see cref="MongoDbTransactionScopeRequiredException"/> instance.</returns>
    public static MongoDbTransactionScopeRequiredException ForMissingScope() =>
        new(
            "No active MongoDbTransactionScope found in the current execution context. " +
            "MongoDB outbox operations require an active transaction scope. " +
            "Ensure you have created a scope using MongoDbTransactionScope.Create(mongoClient) " +
            "and that it hasn't been disposed. For nested operations, the scope must be " +
            "created in a parent context that encompasses all repository operations.");
}
